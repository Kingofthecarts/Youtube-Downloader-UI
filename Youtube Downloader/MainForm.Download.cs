using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Youtube_Downloader;

/// <summary>
/// Partial class containing download logic for MainForm.
/// </summary>
public partial class MainForm
{
    private async void GoButton_Click(object? sender, EventArgs e)
    {
        string url = urlTextBox.Text.Trim();

        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a YouTube URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Check disk space before starting
        if (!CheckDiskSpace())
            return;

        // Clean URL to only include video ID (strip playlist params, etc.)
        url = CleanVideoUrl(url);

        // Extract video ID for cross-history check
        string? videoId = DownloadHistory.ExtractVideoId(url);

        // Check for duplicate in history BEFORE execution (singles and playlist tracks)
        if (!string.IsNullOrEmpty(videoId) && history.HasVideoId(videoId, out string downloadDate))
        {
            var result = MessageBox.Show(
                $"This video was already downloaded on {downloadDate}.\n\n" +
                $"Download again?",
                "Already Downloaded",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                logger.Log($"User skipped duplicate download: {url}");
                return;
            }

            // Mark the old entry as superseded (if it exists in singles)
            var existingRecord = history.FindByUrl(url);
            if (existingRecord != null)
            {
                history.MarkAsSuperseded(videoId);
                logger.Log($"Marked previous download as superseded: {videoId}");
            }
        }

        // Get optional folder name for single video
        string? singleFolder = null;
        string folderInput = singleFolderComboBox.Text.Trim();
        if (!string.IsNullOrEmpty(folderInput))
        {
            // Sanitize folder name
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                folderInput = folderInput.Replace(c, '_');
            }
            singleFolder = folderInput;

            // Add folder to history for future use
            folderHistory.AddOrUpdate(singleFolder);
            RefreshFolderDropdowns();
        }

        await StartDownload(url, false, singleFolder);
    }

    private async void PlaylistGoButton_Click(object? sender, EventArgs e)
    {
        string url = playlistUrlTextBox.Text.Trim();
        string folderName = playlistFolderComboBox.Text.Trim();

        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a Playlist URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(folderName))
        {
            MessageBox.Show("Please enter a folder name for the playlist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Check disk space before starting
        if (!CheckDiskSpace())
            return;

        // Check for duplicate playlist in history BEFORE execution
        string? playlistId = DownloadHistory.ExtractPlaylistId(url);
        if (!string.IsNullOrEmpty(playlistId))
        {
            var existingRecord = history.FindByPlaylistId(playlistId);
            if (existingRecord != null && !existingRecord.IsSuperseded)
            {
                var result = MessageBox.Show(
                    $"This playlist was already downloaded:\n\n" +
                    $"Title: {existingRecord.Title}\n" +
                    $"Date: {existingRecord.DownloadDate:yyyy-MM-dd HH:mm}\n" +
                    $"Items: {existingRecord.PlaylistItemCount}\n" +
                    $"Location: {existingRecord.DownloadFolder}\n\n" +
                    $"Do you want to download it again?\n" +
                    $"(The old entry will be marked as superseded)",
                    "Already Downloaded",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    logger.Log($"User skipped duplicate playlist download: {url}");
                    return;
                }

                // Mark the old entry as superseded
                history.MarkPlaylistAsSuperseded(playlistId);
                logger.Log($"Marked previous playlist download as superseded: {playlistId}");
            }
        }

        // Sanitize folder name
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            folderName = folderName.Replace(c, '_');
        }

        // Add folder to history for future use
        folderHistory.AddOrUpdate(folderName);
        RefreshFolderDropdowns();

        // If we have selected items from browse, download only those
        if (selectedPlaylistItems != null && selectedPlaylistItems.Count > 0)
        {
            await StartSelectedPlaylistDownload(selectedPlaylistItems, excludedPlaylistItems, folderName, url);
            selectedPlaylistItems = null; // Clear after use
            excludedPlaylistItems = null;
        }
        else
        {
            await StartDownload(url, true, folderName);
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel the download?\n\nPartially downloaded files will be deleted.",
                "Cancel Download",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                isCancelling = true;
                cancellationTokenSource.Cancel();
                statusLabel.Text = "Cancelling...";
                cancelButton.Visible = false;
            }
        }
    }

    private async Task StartSelectedPlaylistDownload(List<PlaylistItem> items, List<PlaylistItem>? excludedItems, string folderName, string playlistUrl)
    {
        isPlaylistDownload = true;
        playlistTotal = items.Count;
        playlistCurrent = 0;
        currentPlaylistUrl = playlistUrl;
        currentPlaylistFolder = folderName;
        playlistRecords.Clear();
        isCancelling = false;
        cancellationTokenSource = new CancellationTokenSource();

        // Reset UI
        SetControlsEnabled(false);
        cancelButton.Visible = true;
        downloadProgressBar.Value = 0;
        convertProgressBar.Value = 0;
        // Show progress bars
        downloadProgressBar.Visible = true;
        downloadLabel.Visible = true;
        convertProgressBar.Visible = true;
        convertLabel.Visible = true;
        statusLabel.Text = "Starting...";
        statusLabel.ForeColor = SystemColors.ControlText;
        SetSourceLink(playlistUrl, $"({items.Count} selected)");
        ClearDestinationLink();
        lastDownloadedFile = null;
        currentVideoTitle = null;
        currentVideoId = null;
        currentVideoDurationSeconds = 0;
        currentChannelId = null;
        currentChannelName = null;
        downloadedFileSize = 0;

        // Start job timer
        StartJobTimer();

        logger.LogDownloadStart(playlistUrl, true, folderName);
        logger.Log($"Downloading {items.Count} selected items from playlist");

        string outputFolder = Path.Combine(config.OutputFolder, folderName);
        Directory.CreateDirectory(outputFolder);

        // Show and configure playlist progress bar
        playlistProgressBar.Minimum = 0;
        playlistProgressBar.Maximum = items.Count;
        playlistProgressBar.Value = 0;
        playlistProgressBar.Visible = true;
        playlistLabel.Visible = true;
        playlistProgressLabel.Text = $"0/{items.Count}";
        playlistProgressLabel.Visible = true;

        try
        {
            foreach (var item in items)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                playlistCurrent++;
                currentVideoId = item.VideoId;
                currentVideoTitle = item.Title;
                currentVideoDurationSeconds = 0;
                currentChannelId = null;
                currentChannelName = null;
                downloadedFileSize = 0;
                statusLabel.Text = $"Downloading {playlistCurrent}/{playlistTotal}: {GetTitleForStatus()}";
                downloadProgressBar.Value = 0;
                convertProgressBar.Value = 0;

                // Download this single video to the playlist folder
                await RunYtDlpAsync(item.Url, false, folderName);

                // Update playlist progress bar and label
                playlistProgressBar.Value = playlistCurrent;
                playlistProgressLabel.Text = $"{playlistCurrent}/{playlistTotal}";

                // Track the record and commit to history immediately
                if (!string.IsNullOrEmpty(lastDownloadedFile) && File.Exists(lastDownloadedFile))
                {
                    var fileInfo = new FileInfo(lastDownloadedFile);
                    var record = new DownloadRecord
                    {
                        VideoId = item.VideoId,
                        Url = item.Url,
                        Title = currentVideoTitle ?? Path.GetFileNameWithoutExtension(lastDownloadedFile),
                        DownloadDate = DateTime.Now,
                        FileSizeBytes = fileInfo.Length,
                        FilePath = lastDownloadedFile,
                        DownloadFolder = outputFolder,
                        IsPlaylist = false,
                        PlaylistItemCount = 0,
                        ChannelId = currentChannelId ?? "",
                        ChannelName = currentChannelName ?? ""
                    };
                    playlistRecords.Add(record);

                    // Commit to history immediately if tracking each song
                    if (trackEachSongCheckBox.Checked)
                    {
                        history.AddRecord(record);
                        downloadStats.RecordDownload(record.FileSizeBytes);
                        logger.LogHistoryAdded(record.Title, record.VideoId);
                    }
                }

                // Apply delay between downloads if configured (and not the last item)
                if (config.DownloadDelaySeconds > 0 && playlistCurrent < playlistTotal)
                {
                    await ApplyDownloadDelayAsync();
                }
            }

            if (!isCancelling)
            {
                statusLabel.Text = "Completed";
                statusLabel.ForeColor = Color.Green;
                downloadProgressBar.Value = 100;
                convertProgressBar.Value = 100;
                playlistProgressBar.Value = playlistProgressBar.Maximum;
                SetDestinationLink(outputFolder);
                lastDownloadedFile = outputFolder;

                // Clear input fields on success (but keep progress bars and status)
                ClearInputFields();

                // Add to history
                if (trackEachSongCheckBox.Checked)
                {
                    // Records already committed during download loop - just log completion
                    logger.LogPlaylistComplete(playlistRecords.Count, outputFolder);
                }
                else
                {
                    // Add as single playlist entry
                    long totalSize = playlistRecords.Sum(r => r.FileSizeBytes);
                    string? playlistId = DownloadHistory.ExtractPlaylistId(playlistUrl);

                    // Build playlist tracks list from downloaded records (with playlist index)
                    var playlistTracks = new List<PlaylistTrack>();

                    // Add downloaded items
                    foreach (var item in items)
                    {
                        var downloadedRecord = playlistRecords.FirstOrDefault(r => r.VideoId == item.VideoId);
                        playlistTracks.Add(new PlaylistTrack
                        {
                            VideoId = item.VideoId,
                            Url = item.Url,
                            Title = downloadedRecord?.Title ?? item.Title,
                            FileName = downloadedRecord?.FileName ?? "",
                            Folder = downloadedRecord?.DownloadFolder ?? "",
                            FileSizeBytes = downloadedRecord?.FileSizeBytes ?? 0,
                            IsExcluded = false,
                            PlaylistIndex = item.Index,
                            ChannelId = downloadedRecord?.ChannelId ?? "",
                            ChannelName = downloadedRecord?.ChannelName ?? "",
                            DownloadTimeSeconds = downloadedRecord?.DownloadTimeSeconds ?? 0,
                            ConvertTimeSeconds = downloadedRecord?.ConvertTimeSeconds ?? 0,
                            DownloadDate = downloadedRecord?.DownloadDate ?? DateTime.Now
                        });
                    }

                    // Add excluded items
                    if (excludedItems != null)
                    {
                        foreach (var item in excludedItems)
                        {
                            playlistTracks.Add(new PlaylistTrack
                            {
                                VideoId = item.VideoId,
                                Url = item.Url,
                                Title = item.Title,
                                FileName = "",
                                Folder = "",
                                FileSizeBytes = 0,
                                IsExcluded = true,
                                PlaylistIndex = item.Index,
                                ChannelId = "",
                                ChannelName = "",
                                DownloadDate = DateTime.MinValue
                            });
                        }
                    }

                    // Sort by playlist index
                    playlistTracks = playlistTracks.OrderBy(t => t.PlaylistIndex).ToList();

                    var record = new DownloadRecord
                    {
                        VideoId = playlistId ?? "",
                        Url = playlistUrl,
                        Title = currentPlaylistTitle ?? folderName,
                        DownloadDate = DateTime.Now,
                        FileSizeBytes = totalSize,
                        FilePath = outputFolder,
                        DownloadFolder = outputFolder,
                        IsPlaylist = true,
                        PlaylistItemCount = playlistRecords.Count,
                        PlaylistTracks = playlistTracks,
                        DownloadTimeSeconds = totalDownloadTime.TotalSeconds,
                        ConvertTimeSeconds = totalConvertTime.TotalSeconds
                    };
                    history.AddRecord(record);
                    // Record each song in playlist for stats
                    foreach (var r in playlistRecords)
                    {
                        downloadStats.RecordDownload(r.FileSizeBytes);
                    }
                    logger.LogHistoryAdded(record.Title, $"Playlist: {record.PlaylistItemCount} items ({excludedItems?.Count ?? 0} excluded)");
                    logger.LogPlaylistComplete(playlistRecords.Count, outputFolder);
                }
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Cancelled";
            statusLabel.ForeColor = Color.Orange;
            logger.Log("Download cancelled by user");
            CleanupAfterCancel();
        }
        catch (Exception ex)
        {
            if (!isCancelling)
            {
                statusLabel.Text = "Error: " + ex.Message;
                statusLabel.ForeColor = Color.Red;
                logger.LogError(ex.Message);

                // Check for YouTube authentication error
                if (IsYouTubeAuthError())
                {
                    if (!config.YouTubeLoggedIn)
                    {
                        // Not signed in - prompt to sign in
                        var result = MessageBox.Show(
                            "YouTube requires authentication to download this video.\n\n" +
                            "Would you like to sign in to YouTube now?",
                            "YouTube Sign In Required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            OpenYouTubeLogin();
                        }
                    }
                    else
                    {
                        // Already signed in but still failing - session may be expired
                        var result = MessageBox.Show(
                            "Your YouTube session may have expired.\n\n" +
                            "Would you like to sign in again?",
                            "YouTube Session Expired",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.Yes)
                        {
                            OpenYouTubeLogin();
                        }
                    }
                }
            }
        }
        finally
        {
            StopJobTimer();
            cancelButton.Visible = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            SetControlsEnabled(true);

            // Clean up temporary cookie file (security: don't leave decrypted cookies on disk)
            YouTubeCookieManager.DeleteCookieFile();
        }
    }

    private async Task StartDownload(string url, bool isPlaylist, string? folder)
    {
        isPlaylistDownload = isPlaylist;
        playlistTotal = 0;
        playlistCurrent = 0;
        currentPlaylistUrl = url;
        currentPlaylistFolder = folder;
        playlistRecords.Clear();
        isCancelling = false;
        cancellationTokenSource = new CancellationTokenSource();

        // Reset UI
        SetControlsEnabled(false);
        cancelButton.Visible = true;
        downloadProgressBar.Value = 0;
        convertProgressBar.Value = 0;
        // Show progress bars
        downloadProgressBar.Visible = true;
        downloadLabel.Visible = true;
        convertProgressBar.Visible = true;
        convertLabel.Visible = true;
        statusLabel.Text = "Starting...";
        statusLabel.ForeColor = SystemColors.ControlText;
        SetSourceLink(url);
        ClearDestinationLink();
        lastDownloadedFile = null;
        currentVideoTitle = null;
        currentVideoId = null;
        currentVideoDurationSeconds = 0;
        currentChannelId = null;
        currentChannelName = null;
        downloadedFileSize = 0;
        lastYtDlpOutput = ""; // Reset for auth error detection

        // Start job timer
        StartJobTimer();

        logger.LogDownloadStart(url, isPlaylist, folder);

        string? videoId = DownloadHistory.ExtractVideoId(url);

        try
        {
            await RunYtDlpAsync(url, isPlaylist, folder);

            statusLabel.Text = "Completed";
            statusLabel.ForeColor = Color.Green;
            downloadProgressBar.Value = 100;
            convertProgressBar.Value = 100;

            // Clear input fields on success (but keep progress bars and status)
            ClearInputFields();

            // Determine output folder
            string outputFolder = !string.IsNullOrEmpty(folder)
                ? Path.Combine(config.OutputFolder, folder)
                : config.OutputFolder;

            // Handle history tracking
            if (!isPlaylist)
            {
                // Single video - find and record
                if (string.IsNullOrEmpty(lastDownloadedFile) || !File.Exists(lastDownloadedFile))
                {
                    var mp3Files = Directory.GetFiles(outputFolder, "*.mp3")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    if (mp3Files != null && mp3Files.CreationTime > DateTime.Now.AddMinutes(-5))
                    {
                        lastDownloadedFile = mp3Files.FullName;
                        currentVideoTitle = Path.GetFileNameWithoutExtension(mp3Files.Name);
                        SetDestinationLink(lastDownloadedFile);
                    }
                }

                if (!string.IsNullOrEmpty(lastDownloadedFile) && File.Exists(lastDownloadedFile))
                {
                    // Show rename dialog if checkbox was checked
                    if (allowRenameCheckBox.Checked)
                    {
                        using var renameForm = new RenameForm(lastDownloadedFile, videoId, config.AllowFilenameEdit);
                        if (renameForm.ShowDialog(this) == DialogResult.OK && renameForm.ChangesMade)
                        {
                            // Update to new file path if renamed
                            if (!string.IsNullOrEmpty(renameForm.NewFilePath))
                            {
                                lastDownloadedFile = renameForm.NewFilePath;
                                currentVideoTitle = Path.GetFileNameWithoutExtension(lastDownloadedFile);
                                SetDestinationLink(lastDownloadedFile);
                            }
                        }
                        // RenameForm handles setting video ID in comments
                    }
                    else
                    {
                        // No rename dialog - set video ID in comments now
                        SetVideoIdInComments(lastDownloadedFile, videoId);
                    }

                    var fileInfo = new FileInfo(lastDownloadedFile);
                    var record = new DownloadRecord
                    {
                        VideoId = videoId ?? "",
                        Url = url,
                        Title = currentVideoTitle ?? Path.GetFileNameWithoutExtension(lastDownloadedFile),
                        FileName = Path.GetFileName(lastDownloadedFile),
                        DownloadDate = DateTime.Now,
                        FileSizeBytes = fileInfo.Length,
                        FilePath = lastDownloadedFile,
                        DownloadFolder = outputFolder,
                        IsPlaylist = false,
                        PlaylistItemCount = 0,
                        ChannelId = currentChannelId ?? "",
                        ChannelName = currentChannelName ?? "",
                        DownloadTimeSeconds = totalDownloadTime.TotalSeconds,
                        ConvertTimeSeconds = totalConvertTime.TotalSeconds
                    };
                    history.AddRecord(record);
                    downloadStats.RecordDownload(fileInfo.Length);
                    logger.LogDownloadComplete(lastDownloadedFile, fileInfo.Length);
                    logger.LogHistoryAdded(record.Title, record.VideoId);

                    // Track channel if checkbox was checked
                    if (trackChannelSingleCheckBox.Checked && !string.IsNullOrEmpty(currentChannelId))
                    {
                        await TrackChannelAfterDownloadAsync(currentChannelId, currentChannelName, url);
                    }
                }
            }
            else
            {
                // Playlist handling
                string playlistPath = outputFolder;
                SetDestinationLink(playlistPath);
                lastDownloadedFile = playlistPath;

                if (trackEachSongCheckBox.Checked)
                {
                    // Records already committed during download - just log completion
                    logger.LogPlaylistComplete(playlistRecords.Count, playlistPath);
                }
                else
                {
                    // Add playlist as single entry
                    long totalSize = 0;
                    try
                    {
                        totalSize = Directory.GetFiles(playlistPath, "*.mp3")
                            .Sum(f => new FileInfo(f).Length);
                    }
                    catch { }

                    // Build playlist tracks list from downloaded records
                    var playlistTracks = playlistRecords.Select(r => new PlaylistTrack
                    {
                        VideoId = r.VideoId,
                        Url = r.Url,
                        Title = r.Title,
                        FileName = r.FileName,
                        Folder = r.DownloadFolder,
                        FileSizeBytes = r.FileSizeBytes,
                        ChannelId = r.ChannelId,
                        ChannelName = r.ChannelName,
                        DownloadTimeSeconds = r.DownloadTimeSeconds,
                        ConvertTimeSeconds = r.ConvertTimeSeconds,
                        DownloadDate = r.DownloadDate
                    }).ToList();

                    // Get playlist ID
                    string? playlistId = DownloadHistory.ExtractPlaylistId(url);

                    var record = new DownloadRecord
                    {
                        VideoId = playlistId ?? "",
                        Url = url,
                        Title = currentPlaylistTitle ?? folder!,
                        DownloadDate = DateTime.Now,
                        FileSizeBytes = totalSize,
                        FilePath = playlistPath,
                        DownloadFolder = playlistPath,
                        IsPlaylist = true,
                        PlaylistItemCount = playlistTotal > 0 ? playlistTotal : playlistRecords.Count,
                        PlaylistTracks = playlistTracks,
                        DownloadTimeSeconds = totalDownloadTime.TotalSeconds,
                        ConvertTimeSeconds = totalConvertTime.TotalSeconds
                    };
                    history.AddRecord(record);
                    // Record each song in playlist for stats
                    foreach (var track in playlistTracks)
                    {
                        downloadStats.RecordDownload(track.FileSizeBytes);
                    }
                    logger.LogPlaylistComplete(record.PlaylistItemCount, playlistPath);
                    logger.LogHistoryAdded(record.Title, $"Playlist: {record.PlaylistItemCount} items");
                }

                // Track channel if checkbox was checked (use channel from first playlist record if available)
                if (trackChannelPlaylistCheckBox.Checked)
                {
                    var firstRecord = playlistRecords.FirstOrDefault();
                    string? channelId = firstRecord?.ChannelId ?? currentChannelId;
                    string? channelName = firstRecord?.ChannelName ?? currentChannelName;
                    if (!string.IsNullOrEmpty(channelId))
                    {
                        await TrackChannelAfterDownloadAsync(channelId, channelName, url);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Cancelled";
            statusLabel.ForeColor = Color.Orange;
            logger.Log("Download cancelled by user");
            CleanupAfterCancel();
        }
        catch (Exception ex)
        {
            if (!isCancelling)
            {
                statusLabel.Text = "Error: " + ex.Message;
                statusLabel.ForeColor = Color.Red;
                logger.LogError(ex.Message);
            }
        }
        finally
        {
            StopJobTimer();
            cancelButton.Visible = false;
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            SetControlsEnabled(true);
        }
    }

    /// <summary>
    /// Gets the --cookies argument for yt-dlp if YouTube login is active.
    /// </summary>
    private string GetCookiesArgument()
    {
        var cookiePath = YouTubeCookieManager.GetCookiesFilePath(config);
        return !string.IsNullOrEmpty(cookiePath) ? $"--cookies \"{cookiePath}\" " : "";
    }

    /// <summary>
    /// Checks if the last yt-dlp output indicates a YouTube authentication error.
    /// </summary>
    private bool IsYouTubeAuthError()
    {
        return lastYtDlpOutput.Contains("Sign in to confirm you're not a bot") ||
               lastYtDlpOutput.Contains("--cookies-from-browser") ||
               lastYtDlpOutput.Contains("authentication") ||
               lastYtDlpOutput.Contains("sign in to confirm");
    }

    private async Task FetchChannelInfoAsync(string url)
    {
        try
        {
            string cookiesArg = GetCookiesArgument();
            // Quick fetch of channel info using yt-dlp --print
            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"{cookiesArg}--no-playlist --print channel_id --print channel \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                currentChannelId = lines[0].Trim();
                currentChannelName = lines[1].Trim();
                logger.Log($"Channel info: {currentChannelName} ({currentChannelId})");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to fetch channel info: {ex.Message}");
        }
    }

    private async Task RunYtDlpAsync(string url, bool isPlaylist, string? folder)
    {
        // Fetch channel info before download (for single videos)
        if (!isPlaylist)
        {
            await FetchChannelInfoAsync(url);
        }

        // Determine final output path
        string outputPath;
        if (!string.IsNullOrEmpty(folder))
        {
            // Both playlist and single video with folder go to subfolder
            outputPath = Path.Combine(config.OutputFolder, folder);
            Directory.CreateDirectory(outputPath);
        }
        else
        {
            // Single video without folder goes to root
            outputPath = config.OutputFolder;
        }

        // Use configured temp folder for downloads and conversions
        // Create a unique subfolder within it for this download session
        string tempFolder = Path.Combine(config.TempFolder, "download_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempFolder);
        downloadedFileSize = 0; // Reset for this download

        string arguments;

        // Build thumbnail argument if enabled
        string thumbnailArg = config.EmbedThumbnail ? "--embed-thumbnail " : "";

        // Get cookies argument for YouTube authentication
        string cookiesArg = GetCookiesArgument();

        if (isPlaylist)
        {
            // Playlist download - everything goes to temp folder first
            arguments = $"{cookiesArg}-x --audio-format mp3 {thumbnailArg}" +
                       $"--ffmpeg-location \"{ffmpegPath}\" " +
                       $"-o \"{tempFolder}\\%(title)s.%(ext)s\" " +
                       $"--yes-playlist " +
                       $"\"{url}\"";
        }
        else
        {
            // Single video - everything goes to temp folder first
            arguments = $"{cookiesArg}-x --audio-format mp3 {thumbnailArg}" +
                       $"--ffmpeg-location \"{ffmpegPath}\" " +
                       $"-o \"{tempFolder}\\%(title)s.%(ext)s\" " +
                       $"--no-playlist " +
                       $"\"{url}\"";
        }

        logger.Log($"Command: yt-dlp.exe {arguments}");

        var processInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = config.OutputFolder
        };

        currentProcess = new Process { StartInfo = processInfo };
        filesToCleanup.Clear();
        filesToCleanup.Add(tempFolder); // Track temp folder for cleanup

        currentProcess.OutputDataReceived += (s, e) => ProcessOutput(e.Data, tempFolder);
        currentProcess.ErrorDataReceived += (s, e) => ProcessOutput(e.Data, tempFolder);

        currentProcess.Start();
        currentProcess.BeginOutputReadLine();
        currentProcess.BeginErrorReadLine();

        try
        {
            await currentProcess.WaitForExitAsync(cancellationTokenSource!.Token);
        }
        catch (OperationCanceledException)
        {
            // Kill the process if cancelled
            try
            {
                if (!currentProcess.HasExited)
                {
                    currentProcess.Kill(true);
                    await currentProcess.WaitForExitAsync();
                }
            }
            catch { }
            throw;
        }

        int exitCode = currentProcess.ExitCode;
        currentProcess.Dispose();
        currentProcess = null;

        logger.Log($"yt-dlp exited with code {exitCode}");

        // Move MP3 files from temp folder to output folder
        if (exitCode == 0 && !isCancelling)
        {
            try
            {
                var mp3Files = Directory.GetFiles(tempFolder, "*.mp3");
                foreach (var mp3File in mp3Files)
                {
                    string fileName = Path.GetFileName(mp3File);
                    string destPath = Path.Combine(outputPath, fileName);

                    // If file already exists, add a number suffix
                    if (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        int counter = 1;
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(outputPath, $"{nameWithoutExt} ({counter}){ext}");
                            counter++;
                        }
                    }

                    File.Move(mp3File, destPath);
                    logger.Log($"Moved: {fileName} -> {destPath}");

                    // Update lastDownloadedFile to point to final location
                    lastDownloadedFile = destPath;

                    // Update the destination link with the final path (for single videos)
                    // Must use Invoke since we're on a background thread
                    if (!isPlaylist)
                    {
                        Invoke(() => SetDestinationLink(destPath));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error moving MP3 files: {ex.Message}");
            }
        }

        // Always clean up temp folder after download completes
        try
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
                logger.Log($"Cleaned up temp folder: {tempFolder}");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to clean up temp folder: {ex.Message}");
        }

        if (exitCode != 0 && !isCancelling)
        {
            throw new Exception($"yt-dlp exited with code {exitCode}");
        }
    }

    private void ProcessOutput(string? data, string outputPath)
    {
        if (string.IsNullOrEmpty(data)) return;

        // Log all yt-dlp output
        logger.LogYtDlpOutput(data);

        // Track output for auth error detection
        lastYtDlpOutput += data + "\n";

        Invoke(() =>
        {
            // Parse playlist info: [download] Downloading item X of Y
            var playlistMatch = PlaylistItemRegex.Match(data);
            if (playlistMatch.Success)
            {
                playlistCurrent = int.Parse(playlistMatch.Groups[1].Value);
                playlistTotal = int.Parse(playlistMatch.Groups[2].Value);
                statusLabel.Text = $"Downloading {playlistCurrent} of {playlistTotal}...";

                // Show and update playlist progress bar
                if (!playlistProgressBar.Visible)
                {
                    playlistProgressBar.Minimum = 0;
                    playlistProgressBar.Maximum = playlistTotal;
                    playlistProgressBar.Value = 0;
                    playlistProgressBar.Visible = true;
                    playlistProgressLabel.Visible = true;
                    playlistLabel.Visible = true;
                }
                playlistProgressBar.Value = Math.Min(playlistCurrent, playlistTotal);
                playlistProgressLabel.Text = $"{playlistCurrent}/{playlistTotal}";
            }

            // Parse video ID from youtube extractor: [youtube] VIDEOID: Downloading...
            var videoIdMatch = VideoIdRegex.Match(data);
            if (videoIdMatch.Success)
            {
                currentVideoId = videoIdMatch.Groups[1].Value;
            }

            // Parse download progress: [download]  45.2% of 74.08MiB at 5.23MiB/s
            var progressMatch = ProgressRegex.Match(data);
            if (progressMatch.Success)
            {
                if (double.TryParse(progressMatch.Groups[1].Value, out double percent))
                {
                    downloadProgressBar.Value = Math.Min(100, (int)percent);
                    string titleInfo = GetTitleForStatus();

                    if (isPlaylistDownload && playlistTotal > 0)
                    {
                        statusLabel.Text = $"Downloading {playlistCurrent}/{playlistTotal}: {titleInfo} ({percent:F1}%)";
                    }
                    else
                    {
                        statusLabel.Text = $"Downloading: {titleInfo} ({percent:F1}%)";
                    }

                    // Capture total file size for convert progress estimation
                    if (double.TryParse(progressMatch.Groups[2].Value, out double size))
                    {
                        string unit = progressMatch.Groups[3].Value;
                        long multiplier = unit.StartsWith("G") ? 1024 * 1024 * 1024 :
                                         unit.StartsWith("M") ? 1024 * 1024 :
                                         unit.StartsWith("K") ? 1024 : 1;
                        downloadedFileSize = (long)(size * multiplier);
                    }
                }
            }
            else
            {
                // Fallback: simpler pattern without size
                var simpleProgressMatch = SimpleProgressRegex.Match(data);
                if (simpleProgressMatch.Success)
                {
                    if (double.TryParse(simpleProgressMatch.Groups[1].Value, out double percent))
                    {
                        downloadProgressBar.Value = Math.Min(100, (int)percent);
                    }
                }
            }

            // Parse download destination to get video title and track file for size
            var downloadDestMatch = DownloadDestRegex.Match(data);
            if (downloadDestMatch.Success)
            {
                currentVideoTitle = Path.GetFileNameWithoutExtension(downloadDestMatch.Groups[1].Value);
                string titleInfo = GetTitleForStatus();

                // Start download timing
                if (!isDownloading)
                {
                    isDownloading = true;
                    StartDownloadTiming();
                }

                if (isPlaylistDownload && playlistTotal > 0)
                {
                    statusLabel.Text = $"Downloading {playlistCurrent}/{playlistTotal}: {titleInfo}";
                }
                else
                {
                    statusLabel.Text = $"Downloading: {titleInfo}";
                }
            }

            // Log when download completes
            if (data.Contains("[download] 100%"))
            {
                // Stop download timing
                if (isDownloading)
                {
                    isDownloading = false;
                    StopDownloadTiming();
                }
                logger.Log($"Download complete. Source file size from progress: {downloadedFileSize} bytes");
            }

            // Parse ffmpeg conversion progress - use size= output
            // FFmpeg outputs: size=    1234kB time=00:00:05.00 bitrate= 256.0kbits/s
            var ffmpegSizeMatch = FfmpegSizeRegex.Match(data);
            var ffmpegTimeMatch = FfmpegTimeRegex.Match(data);

            if (ffmpegTimeMatch.Success)
            {
                string titleInfo = GetTitleForStatus();

                if (isPlaylistDownload && playlistTotal > 0)
                {
                    statusLabel.Text = $"Converting {playlistCurrent}/{playlistTotal}: {titleInfo}";
                }
                else
                {
                    statusLabel.Text = $"Converting: {titleInfo}";
                }

                // Calculate progress based on ffmpeg output size vs source file size
                if (ffmpegSizeMatch.Success && downloadedFileSize > 0)
                {
                    if (long.TryParse(ffmpegSizeMatch.Groups[1].Value, out long currentSize))
                    {
                        string unit = ffmpegSizeMatch.Groups[2].Value.ToUpper();
                        long multiplier = unit.StartsWith("M") ? 1024 * 1024 : 1024; // kB or MB
                        long currentBytes = currentSize * multiplier;

                        // MP3 is typically 10-20% of source size, so scale accordingly
                        // Assume MP3 will be ~15% of source, so multiply ratio by ~6.7 to get to 100%
                        double expectedMp3Size = downloadedFileSize * 0.15;
                        double ratio = currentBytes / expectedMp3Size;
                        int progress = (int)(ratio * 90); // Cap at 90%
                        convertProgressBar.Value = Math.Min(90, Math.Max(5, progress));
                    }
                }
                else
                {
                    // Fallback: increment progressively
                    int currentProgress = convertProgressBar.Value;
                    if (currentProgress < 90)
                    {
                        convertProgressBar.Value = currentProgress + 2;
                    }
                }
            }

            // Parse ExtractAudio destination: [ExtractAudio] Destination: filename.mp3
            var destMatch = ExtractAudioDestRegex.Match(data);
            if (destMatch.Success)
            {
                string filename = destMatch.Groups[1].Value;
                if (Path.IsPathRooted(filename))
                {
                    lastDownloadedFile = filename;
                }
                else
                {
                    lastDownloadedFile = Path.Combine(outputPath, filename);
                }

                // Start the convert progress timer and timing
                isConverting = true;
                convertProgressTimer?.Start();
                StartConvertTiming();

                // Don't show "Saved to" here - wait until conversion is complete

                string titleInfo = GetTitleForStatus();

                if (isPlaylistDownload && playlistTotal > 0)
                {
                    statusLabel.Text = $"Converting {playlistCurrent}/{playlistTotal}: {titleInfo}";
                }
                else
                {
                    statusLabel.Text = $"Converting: {titleInfo}";
                }
                convertProgressBar.Value = 10;
            }

            // Check for extraction starting
            if (data.Contains("[ExtractAudio]") && !data.Contains("Destination:"))
            {
                downloadProgressBar.Value = 100;
                string titleInfo = GetTitleForStatus();

                if (isPlaylistDownload && playlistTotal > 0)
                {
                    statusLabel.Text = $"Converting {playlistCurrent}/{playlistTotal}: {titleInfo}";
                }
                else
                {
                    statusLabel.Text = $"Converting: {titleInfo}";
                }
                convertProgressBar.Value = 5;
            }

            // Check for deleting original (means conversion is done for this item)
            if (data.Contains("Deleting original file"))
            {
                // Stop the convert progress timer and timing
                isConverting = false;
                convertProgressTimer?.Stop();
                StopConvertTiming();

                convertProgressBar.Value = 100;
                currentVideoDurationSeconds = 0; // Reset for next video
                downloadedFileSize = 0; // Reset for next video

                // Don't show "Saved to" here for single videos - the file is still in temp folder
                // It will be shown after the file is moved to the final output location

                // Track the record for playlist downloads
                if (isPlaylistDownload && !string.IsNullOrEmpty(lastDownloadedFile))
                {
                    try
                    {
                        if (File.Exists(lastDownloadedFile))
                        {
                            // Set video ID in comments tag for playlist items
                            SetVideoIdInComments(lastDownloadedFile, currentVideoId);

                            var fileInfo = new FileInfo(lastDownloadedFile);
                            string? folder = Path.GetDirectoryName(lastDownloadedFile);
                            // Build individual video URL
                            string videoUrl = !string.IsNullOrEmpty(currentVideoId)
                                ? $"https://www.youtube.com/watch?v={currentVideoId}"
                                : currentPlaylistUrl ?? "";
                            var record = new DownloadRecord
                            {
                                VideoId = currentVideoId ?? "",
                                Url = videoUrl,
                                Title = currentVideoTitle ?? Path.GetFileNameWithoutExtension(lastDownloadedFile),
                                FileName = Path.GetFileName(lastDownloadedFile),
                                DownloadDate = DateTime.Now,
                                FileSizeBytes = fileInfo.Length,
                                FilePath = lastDownloadedFile,
                                DownloadFolder = folder ?? "",
                                IsPlaylist = false,
                                PlaylistItemCount = 0,
                                ChannelId = currentChannelId ?? "",
                                ChannelName = currentChannelName ?? "",
                                DownloadTimeSeconds = currentVideoDownloadTime.TotalSeconds,
                                ConvertTimeSeconds = currentVideoConvertTime.TotalSeconds
                            };
                            playlistRecords.Add(record);

                            // Commit to history immediately if tracking each song
                            if (trackEachSongCheckBox.Checked)
                            {
                                history.AddRecord(record);
                                downloadStats.RecordDownload(record.FileSizeBytes);
                                logger.LogHistoryAdded(record.Title, record.VideoId);
                            }
                        }
                    }
                    catch { }
                }

                // Reset for next item in playlist (only if playlist download)
                if (isPlaylistDownload)
                {
                    downloadProgressBar.Value = 0;
                    convertProgressBar.Value = 0;
                }
            }

            // Check for "already downloaded"
            if (data.Contains("has already been downloaded"))
            {
                statusLabel.Text = "Already downloaded";
                downloadProgressBar.Value = 100;
                convertProgressBar.Value = 100;
            }
        });
    }

    private void CleanupAfterCancel()
    {
        // Stop convert progress timer
        isConverting = false;
        convertProgressTimer?.Stop();

        // Clean up downloaded/converted files
        foreach (var file in filesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    logger.Log($"Cleaned up file: {file}");
                }
                else if (Directory.Exists(file))
                {
                    // Delete all files in temp folder
                    foreach (var f in Directory.GetFiles(file))
                    {
                        File.Delete(f);
                    }
                    Directory.Delete(file);
                    logger.Log($"Cleaned up folder: {file}");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to clean up {file}: {ex.Message}");
            }
        }

        // Also clean up the last downloaded file if it exists
        if (!string.IsNullOrEmpty(lastDownloadedFile) && File.Exists(lastDownloadedFile))
        {
            try
            {
                File.Delete(lastDownloadedFile);
                logger.Log($"Cleaned up: {lastDownloadedFile}");
            }
            catch { }
        }

        filesToCleanup.Clear();

        // Clear all input fields after cancel
        ClearAllButton_Click(null, EventArgs.Empty);
    }

    private string CleanVideoUrl(string url)
    {
        // Extract just the video ID and create a clean URL
        // This strips playlist params, index, etc.
        string? videoId = DownloadHistory.ExtractVideoId(url);

        if (!string.IsNullOrEmpty(videoId))
        {
            string cleanUrl = $"https://www.youtube.com/watch?v={videoId}";
            if (cleanUrl != url)
            {
                logger.Log($"Cleaned URL: {url} -> {cleanUrl}");
            }
            return cleanUrl;
        }

        // If we can't extract video ID, return original
        return url;
    }

    private string GetTitleForStatus()
    {
        if (!string.IsNullOrEmpty(currentVideoTitle))
        {
            // Truncate if too long for status bar
            if (currentVideoTitle.Length > 50)
            {
                return currentVideoTitle.Substring(0, 47) + "...";
            }
            return currentVideoTitle;
        }
        return "...";
    }

    private bool CheckDiskSpace()
    {
        try
        {
            string outputPath = config.OutputFolder;
            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
                return true; // Can't check, proceed anyway

            var driveInfo = new DriveInfo(Path.GetPathRoot(outputPath)!);
            double freeSpacePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;

            if (freeSpacePercent < 10)
            {
                long freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                var result = MessageBox.Show(
                    $"Low disk space warning!\n\n" +
                    $"Drive {driveInfo.Name} has only {freeSpacePercent:F1}% free space ({freeSpaceGB} GB).\n\n" +
                    $"Do you want to continue anyway?",
                    "Low Disk Space",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    logger.Log($"Download cancelled due to low disk space: {freeSpacePercent:F1}% free");
                    return false;
                }
                logger.Log($"User continued despite low disk space: {freeSpacePercent:F1}% free");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to check disk space: {ex.Message}");
        }
        return true;
    }

    private async Task ApplyDownloadDelayAsync()
    {
        int delaySeconds = config.DownloadDelaySeconds;
        logger.Log($"Applying delay of {delaySeconds} seconds before next download");

        for (int remaining = delaySeconds; remaining > 0; remaining--)
        {
            if (cancellationTokenSource?.IsCancellationRequested == true)
                break;

            int minutes = remaining / 60;
            int seconds = remaining % 60;
            statusLabel.Text = $"Waiting {minutes}:{seconds:D2} before next download...";
            statusLabel.ForeColor = Color.Blue;

            try
            {
                await Task.Delay(1000, cancellationTokenSource!.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        statusLabel.ForeColor = SystemColors.ControlText;
    }

    /// <summary>
    /// Sets the video ID in the MP3 file's comments tag.
    /// Called after conversion completes to ensure all downloads have the video ID stored.
    /// </summary>
    private void SetVideoIdInComments(string filePath, string? videoId)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(videoId) || !File.Exists(filePath))
            return;

        try
        {
            using var file = TagLib.File.Create(filePath);
            // Only set if comments is empty or doesn't already contain the video ID
            var existingComment = file.Tag.Comment ?? "";
            if (string.IsNullOrEmpty(existingComment))
            {
                file.Tag.Comment = videoId;
                file.Save();
                logger.Log($"Set video ID in comments: {videoId}");
            }
            else if (!existingComment.Contains(videoId))
            {
                // Don't overwrite existing comments, but log that we skipped
                logger.Log($"Skipped setting video ID - comments already has content: {existingComment}");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to set video ID in comments: {ex.Message}");
        }
    }

    /// <summary>
    /// Track a channel after download completes if not already monitored.
    /// </summary>
    private async Task TrackChannelAfterDownloadAsync(string channelId, string? channelName, string sourceUrl)
    {
        if (string.IsNullOrEmpty(channelId))
            return;

        // Check if already monitored
        if (IsChannelMonitored(channelId))
        {
            logger.Log($"Channel already monitored: {channelName ?? channelId}");
            return;
        }

        // Build channel URL from channel ID
        string channelUrl = $"https://www.youtube.com/channel/{channelId}/videos";

        logger.Log($"Adding channel to monitor: {channelName ?? channelId}");

        // Add the channel (this will open the Channel Monitor form)
        await AddChannelToMonitorAsync(channelId, channelName ?? channelId, channelUrl);
    }
}
