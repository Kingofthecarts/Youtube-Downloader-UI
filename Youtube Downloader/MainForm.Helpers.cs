using System.Diagnostics;

namespace Youtube_Downloader;

/// <summary>
/// Partial class containing helper/utility methods for MainForm.
/// </summary>
public partial class MainForm
{
    private void SetControlsEnabled(bool enabled)
    {
        // Disable tab switching during download
        downloadTabControl.Enabled = enabled;

        // Only enable Go button if also URL is not empty
        goButton.Enabled = enabled && !string.IsNullOrWhiteSpace(urlTextBox.Text);
        singleFolderComboBox.Enabled = enabled;
        singleClearFolderButton.Enabled = enabled;
        bool hasPlaylistUrl = !string.IsNullOrWhiteSpace(playlistUrlTextBox.Text);
        playlistGoButton.Enabled = enabled && hasPlaylistUrl && !string.IsNullOrWhiteSpace(playlistFolderComboBox.Text);
        playlistBrowseButton.Enabled = enabled && hasPlaylistUrl;
        urlTextBox.Enabled = enabled;
        playlistUrlTextBox.Enabled = enabled;
        playlistFolderComboBox.Enabled = enabled;
        trackEachSongCheckBox.Enabled = enabled;
        clearAllButton.Visible = enabled;
        cancelButton.Visible = !enabled;

        // Hide playlist progress bar when not downloading
        if (enabled)
        {
            playlistProgressBar.Visible = false;
            playlistProgressLabel.Visible = false;
            playlistLabel.Visible = false;
        }
    }

    private void UpdateGoButtonEnabled()
    {
        // Only enable Go button if tools are ready AND URL is not empty
        goButton.Enabled = toolsManager.AreToolsAvailable() && !string.IsNullOrWhiteSpace(urlTextBox.Text);
    }

    private void RefreshFolderDropdowns()
    {
        // Reload from disk to get latest
        folderHistory.Load();

        var folders = folderHistory.GetSortedFolderNames(config.SortFoldersByRecent);

        // Save current text values
        string singleText = singleFolderComboBox.Text;
        string playlistText = playlistFolderComboBox.Text;

        // Update both combo boxes
        singleFolderComboBox.BeginUpdate();
        playlistFolderComboBox.BeginUpdate();

        singleFolderComboBox.Items.Clear();
        playlistFolderComboBox.Items.Clear();

        foreach (var folder in folders)
        {
            singleFolderComboBox.Items.Add(folder);
            playlistFolderComboBox.Items.Add(folder);
        }

        singleFolderComboBox.EndUpdate();
        playlistFolderComboBox.EndUpdate();

        // Restore text values
        singleFolderComboBox.Text = singleText;
        playlistFolderComboBox.Text = playlistText;
    }

    private void ConvertProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!isConverting || string.IsNullOrEmpty(lastDownloadedFile))
            return;

        try
        {
            if (File.Exists(lastDownloadedFile) && downloadedFileSize > 0)
            {
                var mp3Info = new FileInfo(lastDownloadedFile);
                // MP3 is typically 10-20% of source size
                double expectedMp3Size = downloadedFileSize * 0.15;
                double ratio = mp3Info.Length / expectedMp3Size;
                int progress = (int)(ratio * 90); // Cap at 90%
                convertProgressBar.Value = Math.Min(90, Math.Max(convertProgressBar.Value, progress));
            }
        }
        catch { }
    }

    private void JobTimer_Tick(object? sender, EventArgs e)
    {
        // Update total time
        var elapsed = DateTime.Now - jobStartTime;
        totalTimeLabel.Text = $"Total: {FormatTimeSpan(elapsed)}";

        // Update download time while downloading
        if (isDownloading)
        {
            var currentDownload = totalDownloadTime + (DateTime.Now - downloadStartTime);
            downloadTimeLabel.Text = $"DL: {FormatTimeSpan(currentDownload)}";
        }

        // Update convert time while converting
        if (isConverting)
        {
            var currentConvert = totalConvertTime + (DateTime.Now - convertStartTime);
            convertTimeLabel.Text = $"Conv: {FormatTimeSpan(currentConvert)}";
        }
    }

    private void StatsTimer_Tick(object? sender, EventArgs e)
    {
        UpdateSystemStats();
    }

    private void UpdateSystemStats()
    {
        try
        {
            // CPU usage using PerformanceCounter (no allocations per call)
            if (cpuCounter != null)
            {
                float cpuPercent = cpuCounter.NextValue();
                cpuLabel.Text = $"CPU: {cpuPercent:F0}%";
            }

            // Memory usage: calculate from available memory
            if (availableMemCounter != null && totalPhysicalMemory > 0)
            {
                float availableMB = availableMemCounter.NextValue();
                long availableBytes = (long)(availableMB * 1024 * 1024);
                double usedPercent = (1.0 - (double)availableBytes / totalPhysicalMemory) * 100.0;
                memoryLabel.Text = $"Mem: {Math.Max(0, Math.Min(100, usedPercent)):F0}%";
            }

            // Drive free space
            string outputPath = config.OutputFolder;
            if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(outputPath)!);
                double freePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;
                driveLabel.Text = $"{driveInfo.Name} {freePercent:F0}% free";
            }
            else
            {
                driveLabel.Text = "Drive: N/A";
            }
        }
        catch
        {
            // Silently ignore errors
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private void StartJobTimer()
    {
        jobStartTime = DateTime.Now;
        downloadStartTime = DateTime.Now;
        totalDownloadTime = TimeSpan.Zero;
        totalConvertTime = TimeSpan.Zero;
        currentVideoDownloadTime = TimeSpan.Zero;
        currentVideoConvertTime = TimeSpan.Zero;
        isDownloading = true;
        isConverting = false;
        downloadTimeLabel.Text = "DL: 0:00";
        convertTimeLabel.Text = "Conv: 0:00";
        totalTimeLabel.Text = "Total: 0:00";
        jobTimer?.Start();
    }

    private void StopJobTimer()
    {
        jobTimer?.Stop();
        var totalElapsed = DateTime.Now - jobStartTime;
        totalTimeLabel.Text = $"Total: {FormatTimeSpan(totalElapsed)}";
    }

    private void StartDownloadTiming()
    {
        downloadStartTime = DateTime.Now;
        currentVideoDownloadTime = TimeSpan.Zero;
    }

    private void StopDownloadTiming()
    {
        currentVideoDownloadTime = DateTime.Now - downloadStartTime;
        totalDownloadTime += currentVideoDownloadTime;
        downloadTimeLabel.Text = $"DL: {FormatTimeSpan(totalDownloadTime)}";
    }

    private void StartConvertTiming()
    {
        convertStartTime = DateTime.Now;
        currentVideoConvertTime = TimeSpan.Zero;
    }

    private void StopConvertTiming()
    {
        currentVideoConvertTime = DateTime.Now - convertStartTime;
        totalConvertTime += currentVideoConvertTime;
        convertTimeLabel.Text = $"Conv: {FormatTimeSpan(totalConvertTime)}";
    }

    // === Menu Item Click Handlers ===

    private void OpenOutputFolderMenuItem_Click(object? sender, EventArgs e)
    {
        if (Directory.Exists(config.OutputFolder))
        {
            Process.Start("explorer.exe", config.OutputFolder);
        }
        else
        {
            MessageBox.Show("Output folder does not exist.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Application.Exit();
    }

    private void HistoryMenuItem_Click(object? sender, EventArgs e)
    {
        logger.Log("Opening history window");
        var stopwatch = Stopwatch.StartNew();
        using var historyForm = new HistoryForm(history, downloadStats, this);
        historyForm.ShowDialog(this);
        stopwatch.Stop();

        if (config.EnablePerformanceTracking)
        {
            config.LastHistoryOpenMs = (int)stopwatch.ElapsedMilliseconds;
            config.Save();
            logger.Log($"History window open time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private void ClearHistoryMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all download history?\n\nThis will not delete the downloaded files.",
            "Clear History",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            history.ClearAll();
            MessageBox.Show("Download history has been cleared.", "History Cleared",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void BrowseSongsMenuItem_Click(object? sender, EventArgs e)
    {
        // If song browser is already open, bring it to front
        if (openSongBrowser != null && !openSongBrowser.IsDisposed)
        {
            openSongBrowser.BringToFront();
            openSongBrowser.Focus();
            return;
        }

        if (!Directory.Exists(config.OutputFolder))
        {
            MessageBox.Show("Output folder does not exist.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        openSongBrowser = new SongBrowserForm(config.OutputFolder, config, this);
        openSongBrowser.FormClosed += (s, args) => { openSongBrowser = null; };
        openSongBrowser.Show();  // Non-modal so user can still use main window
        stopwatch.Stop();

        if (config.EnablePerformanceTracking)
        {
            config.LastSongBrowserOpenMs = (int)stopwatch.ElapsedMilliseconds;
            config.Save();
            logger.Log($"Song Browser open time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private void OptionsMenuItem_Click(object? sender, EventArgs e)
    {
        // Prevent Options and Edit Config from being open simultaneously
        if (isOptionsOrEditConfigOpen)
        {
            MessageBox.Show(
                "Please close the current Options or Edit Configuration window first.",
                "Window Already Open",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        isOptionsOrEditConfigOpen = true;
        try
        {
            using var optionsForm = new OptionsForm(config, toolsManager, logger);
            if (optionsForm.ShowDialog(this) == DialogResult.OK)
            {
                logger.Log("Options saved");

                // Refresh folder dropdowns in case sort order changed
                RefreshFolderDropdowns();

                // Update OneDrive monitoring based on new config
                if (config.MonitorOneDrive && oneDriveTimer == null)
                {
                    CheckOneDriveStatus(showSuccessMessage: false);
                    StartOneDriveMonitoring();
                }
                else if (!config.MonitorOneDrive && oneDriveTimer != null)
                {
                    StopOneDriveMonitoring();
                }

                // Update channel scan timer interval if changed
                if (channelScanTimer != null)
                {
                    channelScanTimer.Interval = config.ChannelScanIntervalMinutes * 60 * 1000;
                }
            }
        }
        finally
        {
            isOptionsOrEditConfigOpen = false;
        }
    }

    private void EditConfigMenuItem_Click(object? sender, EventArgs e)
    {
        // Prevent Options and Edit Config from being open simultaneously
        if (isOptionsOrEditConfigOpen)
        {
            MessageBox.Show(
                "Please close the current Options or Edit Configuration window first.",
                "Window Already Open",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        isOptionsOrEditConfigOpen = true;
        try
        {
            using var editForm = new EditConfigForm(toolsManager, config, history);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                // Check if restart was requested (config reset)
                if (editForm.RestartRequested)
                {
                    logger.Log("Config reset requested - restarting application");
                    RestartApplication();
                    return;
                }

                if (editForm.ConfigChanged)
                {
                    // Update local paths from tools manager
                    ytDlpPath = toolsManager.YtDlpPath;
                    ffmpegPath = toolsManager.FfmpegPath;

                    logger.Log($"Config updated - Output: {config.OutputFolder}, yt-dlp: {ytDlpPath}, ffmpeg: {ffmpegPath}");

                    // Refresh folder dropdowns in case sort order changed
                    RefreshFolderDropdowns();

                    // Update OneDrive monitoring based on new config
                    if (config.MonitorOneDrive && oneDriveTimer == null)
                    {
                        CheckOneDriveStatus(showSuccessMessage: false);
                        StartOneDriveMonitoring();
                    }
                    else if (!config.MonitorOneDrive && oneDriveTimer != null)
                    {
                        StopOneDriveMonitoring();
                    }
                }
            }
        }
        finally
        {
            isOptionsOrEditConfigOpen = false;
        }
    }

    private void FolderListMenuItem_Click(object? sender, EventArgs e)
    {
        using var editorForm = new FolderListEditorForm(folderHistory);
        editorForm.ShowDialog(this);

        if (editorForm.ChangesMade)
        {
            RefreshFolderDropdowns();
        }
    }

    private void ChangelogMenuItem_Click(object? sender, EventArgs e)
    {
        using var changelogForm = new ChangelogForm();
        changelogForm.ShowDialog(this);
    }

    private async void CheckForUpdatesMenuItem_Click(object? sender, EventArgs e)
    {
        string repoUrl = config.GitHubReleaseUrl;

        if (string.IsNullOrEmpty(repoUrl))
        {
            var result = MessageBox.Show(
                "No GitHub repository URL is configured.\n\n" +
                "Would you like to open the options to set one?\n\n" +
                "The URL should be your GitHub repository (e.g., https://github.com/user/repo).",
                "Repository URL Not Set",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                EditConfigMenuItem_Click(sender, e);
            }
            return;
        }

        logger.Log($"Checking for updates from: {repoUrl}");
        logger.Log($"Current version: {AppUpdater.CurrentVersionString}");

        // Check for updates first
        statusLabel.Text = "Checking for updates...";
        var updateResult = await AppUpdater.CheckForUpdateAsync(repoUrl);

        if (!string.IsNullOrEmpty(updateResult.ErrorMessage))
        {
            statusLabel.Text = "Update check failed";
            logger.Log($"Update check error: {updateResult.ErrorMessage}");
            MessageBox.Show(
                $"Failed to check for updates:\n\n{updateResult.ErrorMessage}",
                "Update Check Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        logger.Log($"Latest version: {updateResult.LatestVersion}");

        if (!updateResult.UpdateAvailable)
        {
            statusLabel.Text = "Up to date";
            MessageBox.Show(
                $"You are running the latest version.\n\n" +
                $"Current version: {AppUpdater.CurrentVersionString}\n" +
                $"Latest version: {updateResult.LatestVersion}",
                "No Updates Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Update available - ask user if they want to download
        var downloadConfirm = MessageBox.Show(
            $"A new version is available!\n\n" +
            $"Current version: {AppUpdater.CurrentVersionString}\n" +
            $"Latest version: {updateResult.LatestVersion}\n\n" +
            (string.IsNullOrEmpty(updateResult.ReleaseNotes) ? "" : $"Release notes:\n{updateResult.ReleaseNotes}\n\n") +
            "Do you want to download and install the update?",
            "Update Available",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (downloadConfirm != DialogResult.Yes)
        {
            logger.Log("User declined update");
            return;
        }

        logger.Log($"Downloading update from: {updateResult.DownloadUrl}");

        // Show progress dialog in update mode
        using var progressForm = new DownloadProgressForm("Downloading Update", updateMode: true);

        // Set up the upgrade action
        progressForm.SetUpgradeAction(() =>
        {
            try
            {
                logger.Log("Launching update script and exiting");
                statusLabel.Text = "Installing update...";
                AppUpdater.LaunchUpdateScript();
                Application.Exit();
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to launch update script: {ex.Message}");
                MessageBox.Show(
                    $"Failed to install update:\n{ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        });

        var downloadTask = progressForm.RunDownloadAsync(async (progress, token) =>
        {
            var intProgress = new Progress<int>(percent =>
            {
                progress.Report($"Downloading v{updateResult.LatestVersion}... {percent}%");
            });

            return await AppUpdater.CheckAndDownloadUpdateAsync(updateResult.DownloadUrl!, intProgress);
        });

        progressForm.ShowDialog(this);
        await downloadTask;

        // If download succeeded but user didn't click "Install Update", just log it
        if (progressForm.WasSuccessful)
        {
            logger.Log("Update downloaded - waiting for user to install");
            statusLabel.Text = "Update ready - restart to install";
        }
        else if (!progressForm.WasCancelled)
        {
            logger.Log("Update download failed");
            MessageBox.Show(
                "Failed to download the update.\n\n" +
                "Please try again later.",
                "Download Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        else
        {
            logger.Log("Update download cancelled by user");
        }
    }

    private async void RedownloadFfmpegMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "This will delete the existing ffmpeg and download a fresh copy.\n\nContinue?",
            "Redownload ffmpeg",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            using var downloadForm = new DownloadProgressForm("Redownloading ffmpeg");

            // Start the download in the background
            var downloadTask = downloadForm.RunDownloadAsync(async (progress, token) =>
            {
                return await toolsManager.RedownloadFfmpegAsync(progress);
            });

            downloadForm.ShowDialog(this);
            await downloadTask;

            if (downloadForm.WasSuccessful)
            {
                ffmpegPath = toolsManager.FfmpegPath;
                statusLabel.Text = "ffmpeg redownloaded successfully";
                logger.Log($"ffmpeg redownloaded to: {ffmpegPath}");
                EnableButtonsIfToolsReady();
            }
            else if (!downloadForm.WasCancelled)
            {
                statusLabel.Text = "ffmpeg redownload failed";
            }
        }
    }

    private async void RedownloadYtDlpMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "This will delete the existing yt-dlp and download a fresh copy.\n\nContinue?",
            "Redownload yt-dlp",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            using var downloadForm = new DownloadProgressForm("Redownloading yt-dlp");

            // Start the download in the background
            var downloadTask = downloadForm.RunDownloadAsync(async (progress, token) =>
            {
                return await toolsManager.RedownloadYtDlpAsync(progress);
            });

            downloadForm.ShowDialog(this);
            await downloadTask;

            if (downloadForm.WasSuccessful)
            {
                ytDlpPath = toolsManager.YtDlpPath;
                statusLabel.Text = "yt-dlp redownloaded successfully";
                logger.Log($"yt-dlp redownloaded to: {ytDlpPath}");
                EnableButtonsIfToolsReady();
            }
            else if (!downloadForm.WasCancelled)
            {
                statusLabel.Text = "yt-dlp redownload failed";
            }
        }
    }

    private async void RedownloadDenoMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "This will delete the existing Deno runtime and download a fresh copy.\n\n" +
            "Deno is used by yt-dlp to handle YouTube's JavaScript protections.\n\nContinue?",
            "Redownload Deno",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            using var downloadForm = new DownloadProgressForm("Redownloading Deno");

            var downloadTask = downloadForm.RunDownloadAsync(async (progress, token) =>
            {
                return await toolsManager.RedownloadDenoAsync(progress);
            });

            downloadForm.ShowDialog(this);
            await downloadTask;

            if (downloadForm.WasSuccessful)
            {
                statusLabel.Text = "Deno redownloaded successfully";
                logger.Log($"Deno redownloaded to: {toolsManager.DenoPath}");
            }
            else if (!downloadForm.WasCancelled)
            {
                statusLabel.Text = "Deno redownload failed";
            }
        }
    }

    private async void ReinstallWebView2MenuItem_Click(object? sender, EventArgs e)
    {
        // Check if already installed
        string? currentVersion = ToolsManager.GetWebView2Version();
        string message = currentVersion != null
            ? $"WebView2 Runtime is currently installed (v{currentVersion}).\n\n" +
              "This will download and reinstall the WebView2 Runtime.\n\n" +
              "Note: This requires administrator privileges.\n\nContinue?"
            : "WebView2 Runtime is not installed.\n\n" +
              "This will download and install the WebView2 Runtime.\n\n" +
              "Note: This requires administrator privileges.\n\nContinue?";

        var result = MessageBox.Show(
            message,
            "Install WebView2 Runtime",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            using var downloadForm = new DownloadProgressForm("Installing WebView2 Runtime");

            var downloadTask = downloadForm.RunDownloadAsync(async (progress, token) =>
            {
                return await toolsManager.InstallWebView2Async(progress);
            });

            downloadForm.ShowDialog(this);
            await downloadTask;

            if (downloadForm.WasSuccessful)
            {
                statusLabel.Text = "WebView2 Runtime installed successfully";
                logger.Log("WebView2 Runtime installed successfully");
                MessageBox.Show(
                    "WebView2 Runtime has been installed successfully.\n\n" +
                    "YouTube sign-in functionality is now available.",
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!downloadForm.WasCancelled)
            {
                statusLabel.Text = "WebView2 installation failed";
                MessageBox.Show(
                    "Failed to install WebView2 Runtime.\n\n" +
                    "Please try running the application as administrator,\n" +
                    "or download WebView2 manually from Microsoft.",
                    "Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private void YouTubeSignInMenuItem_Click(object? sender, EventArgs e)
    {
        OpenYouTubeLogin();
    }

    private void YouTubeSignOutMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "This will sign you out of YouTube and clear saved login data.\n\n" +
            "You may experience download restrictions until you sign in again.\n\nContinue?",
            "Sign Out of YouTube",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            YouTubeCookieManager.ClearCookies(config);
            UpdateYouTubeStatus();
            statusLabel.Text = "Signed out of YouTube";
            logger.Log("User signed out of YouTube");
        }
    }

    private void OpenYouTubeLogin()
    {
        // Check if WebView2 is installed
        if (!ToolsManager.IsWebView2Available())
        {
            var result = MessageBox.Show(
                "WebView2 Runtime is not installed.\n\n" +
                "WebView2 is required for YouTube sign-in.\n\n" +
                "Would you like to install it now?",
                "WebView2 Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                ReinstallWebView2MenuItem_Click(null, EventArgs.Empty);
            }
            return;
        }

        using var loginForm = new YouTubeLoginForm(config);
        if (loginForm.ShowDialog(this) == DialogResult.OK && loginForm.LoginSuccessful)
        {
            UpdateYouTubeStatus();
            statusLabel.Text = "Signed in to YouTube";
            logger.Log("User signed in to YouTube");
        }
    }

    private void UpdateYouTubeStatus()
    {
        if (config.YouTubeLoggedIn)
        {
            string displayText = string.IsNullOrEmpty(config.YouTubeLoginEmail)
                ? "YouTube: Signed in"
                : $"YouTube: {config.YouTubeLoginEmail}";
            youtubeStatusLabel.Text = displayText;
            youtubeStatusLabel.ForeColor = Color.Green;
            youtubeSignInMenuItem.Text = "Sign in to YouTube... (Signed in)";
            youtubeSignOutMenuItem.Visible = true;
        }
        else
        {
            youtubeStatusLabel.Text = "YouTube: Not signed in";
            youtubeStatusLabel.ForeColor = Color.Gray;
            youtubeSignInMenuItem.Text = "Sign in to YouTube...";
            youtubeSignOutMenuItem.Visible = false;
        }
    }

    private void EnableButtonsIfToolsReady()
    {
        if (toolsManager.AreToolsAvailable())
        {
            ytDlpPath = toolsManager.YtDlpPath;
            ffmpegPath = toolsManager.FfmpegPath;
            UpdateGoButtonEnabled();
            playlistGoButton.Enabled = true;
            playlistBrowseButton.Enabled = true;
            statusLabel.Text = "Ready";
            logger.Log("All tools now available - buttons enabled");
        }
    }

    // === Song Browser Methods ===

    public void OpenSongBrowserAndPlay(string filePath)
    {
        if (!Directory.Exists(config.OutputFolder))
        {
            MessageBox.Show("Output folder does not exist.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // If song browser is already open, use it; otherwise create new one
        if (openSongBrowser == null || openSongBrowser.IsDisposed)
        {
            var stopwatch = Stopwatch.StartNew();
            openSongBrowser = new SongBrowserForm(config.OutputFolder, config, this);
            openSongBrowser.FormClosed += (s, args) => { openSongBrowser = null; };
            openSongBrowser.Show();
            stopwatch.Stop();

            if (config.EnablePerformanceTracking)
            {
                config.LastSongBrowserOpenMs = (int)stopwatch.ElapsedMilliseconds;
                config.Save();
                logger.Log($"Song Browser open time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        else
        {
            openSongBrowser.BringToFront();
            openSongBrowser.Focus();
        }

        // Play the specified file
        openSongBrowser.PlayFile(filePath);
    }

    /// <summary>
    /// Open song browser and try to play a song by its video ID stored in comments.
    /// Returns true if a matching song was found and is playing.
    /// </summary>
    public bool OpenSongBrowserAndPlayByVideoId(string videoId)
    {
        if (!Directory.Exists(config.OutputFolder))
        {
            return false;
        }

        if (openSongBrowser == null || openSongBrowser.IsDisposed)
        {
            var stopwatch = Stopwatch.StartNew();
            openSongBrowser = new SongBrowserForm(config.OutputFolder, config, this);
            openSongBrowser.FormClosed += (s, args) => { openSongBrowser = null; };
            openSongBrowser.Show();
            stopwatch.Stop();

            if (config.EnablePerformanceTracking)
            {
                config.LastSongBrowserOpenMs = (int)stopwatch.ElapsedMilliseconds;
                config.Save();
                logger.Log($"Song Browser open time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        else
        {
            openSongBrowser.BringToFront();
            openSongBrowser.Focus();
        }

        return openSongBrowser.PlayByVideoId(videoId);
    }

    // === Other UI Handlers ===

    private void PlaylistUrlTextBox_TextChanged(object? sender, EventArgs e)
    {
        bool hasUrl = !string.IsNullOrWhiteSpace(playlistUrlTextBox.Text);
        playlistBrowseButton.Enabled = hasUrl;
        // Go button needs both URL and folder name
        playlistGoButton.Enabled = hasUrl && !string.IsNullOrWhiteSpace(playlistFolderComboBox.Text);
    }

    private void PlaylistFolderComboBox_TextChanged(object? sender, EventArgs e)
    {
        // Enable playlist Go button only if both URL and folder name are provided
        bool hasUrl = !string.IsNullOrWhiteSpace(playlistUrlTextBox.Text);
        playlistGoButton.Enabled = hasUrl && !string.IsNullOrWhiteSpace(playlistFolderComboBox.Text);
    }

    private void PlaylistBrowseButton_Click(object? sender, EventArgs e)
    {
        string url = playlistUrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a Playlist URL first.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Check for duplicate playlist in history before loading
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
                    $"Items: {existingRecord.PlaylistItemCount}\n\n" +
                    $"Do you want to download it again?\n" +
                    $"(The old entry will be marked as superseded)",
                    "Already Downloaded",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    logger.Log($"User skipped duplicate playlist: {url}");
                    return;
                }

                // Mark the old entry as superseded
                history.MarkPlaylistAsSuperseded(playlistId);
                logger.Log($"Marked previous playlist download as superseded: {playlistId}");
            }
        }

        logger.Log($"Opening playlist selector for: {url}");

        using var selectorForm = new PlaylistSelectorForm(ytDlpPath, url, config);
        selectorForm.ShowDialog(this);

        if (selectorForm.DialogConfirmed && selectorForm.SelectedItems.Count > 0)
        {
            selectedPlaylistItems = selectorForm.SelectedItems;
            excludedPlaylistItems = selectorForm.ExcludedItems;
            currentPlaylistTitle = selectorForm.PlaylistTitle;

            logger.Log($"Playlist title from YouTube: {currentPlaylistTitle}");
            logger.Log($"Selected {selectedPlaylistItems.Count} items from playlist");

            // Log excluded tracks
            if (excludedPlaylistItems.Count > 0)
            {
                logger.Log($"Excluded {excludedPlaylistItems.Count} tracks from playlist:");
                foreach (var item in excludedPlaylistItems)
                {
                    logger.Log($"  - Excluded: [{item.Index}] {item.Title} ({item.VideoId})");
                }
            }

            // Prompt for folder name if empty
            if (string.IsNullOrWhiteSpace(playlistFolderComboBox.Text))
            {
                var result = MessageBox.Show(
                    $"Would you like to assign a folder name for this playlist?\n\n" +
                    $"Playlist: {currentPlaylistTitle ?? "Unknown"}\n" +
                    $"Selected: {selectedPlaylistItems.Count} items",
                    "Assign Folder Name?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Suggest playlist title as default folder name
                    string suggestedName = SanitizeFolderName(currentPlaylistTitle ?? "Playlist");
                    string? folderName = ShowInputDialog("Enter Folder Name", "Folder name:", suggestedName);
                    if (!string.IsNullOrWhiteSpace(folderName))
                    {
                        playlistFolderComboBox.Text = SanitizeFolderName(folderName);
                    }
                }
            }
        }
        else
        {
            selectedPlaylistItems = null;
            excludedPlaylistItems = null;
            currentPlaylistTitle = null;
        }
    }

    private void DestinationLink_Click(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (!string.IsNullOrEmpty(lastDownloadedFile))
        {
            if (File.Exists(lastDownloadedFile))
            {
                Process.Start("explorer.exe", $"/select,\"{lastDownloadedFile}\"");
            }
            else if (Directory.Exists(lastDownloadedFile))
            {
                Process.Start("explorer.exe", lastDownloadedFile);
            }
            else
            {
                string? folder = Path.GetDirectoryName(lastDownloadedFile);
                if (folder != null && Directory.Exists(folder))
                {
                    Process.Start("explorer.exe", folder);
                }
            }
        }
        else if (Directory.Exists(config.OutputFolder))
        {
            Process.Start("explorer.exe", config.OutputFolder);
        }
    }

    private string? currentSourceUrl;

    private void SourceLink_Click(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentSourceUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentSourceUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to open URL: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sets the source link with "Source: " prefix (not clickable) and URL (clickable)
    /// </summary>
    private void SetSourceLink(string url, string? suffix = null)
    {
        currentSourceUrl = url;
        string prefix = "Source: ";
        string displayText = suffix != null ? $"{prefix}{url} {suffix}" : $"{prefix}{url}";

        sourceLink.Text = displayText;
        sourceLink.Links.Clear();
        // Only make the URL portion clickable (after "Source: ")
        sourceLink.Links.Add(prefix.Length, url.Length);
    }

    /// <summary>
    /// Clears the source link
    /// </summary>
    private void ClearSourceLink()
    {
        currentSourceUrl = null;
        sourceLink.Text = "";
        sourceLink.Links.Clear();
    }

    /// <summary>
    /// Sets the destination link with "Saved to: " prefix (not clickable) and path (clickable)
    /// </summary>
    private void SetDestinationLink(string path)
    {
        string prefix = "Saved to: ";
        destinationLink.Text = $"{prefix}{path}";
        destinationLink.Links.Clear();
        // Only make the path portion clickable (after "Saved to: ")
        destinationLink.Links.Add(prefix.Length, path.Length);

        // Show Play button only for single downloads (not playlists) and only for audio files
        bool isAudioFile = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
        playSongButton.Visible = !isPlaylistDownload && isAudioFile && File.Exists(path);
    }

    /// <summary>
    /// Clears the destination link
    /// </summary>
    private void ClearDestinationLink()
    {
        destinationLink.Text = "";
        destinationLink.Links.Clear();
        playSongButton.Visible = false;
    }

    /// <summary>
    /// Opens the song browser and plays the last downloaded file
    /// </summary>
    private void PlaySongButton_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(lastDownloadedFile) && File.Exists(lastDownloadedFile))
        {
            OpenSongBrowserAndPlay(lastDownloadedFile);
        }
    }

    private void ClearAllButton_Click(object? sender, EventArgs e)
    {
        ClearInputFields();
        statusLabel.Text = "Ready";
        statusLabel.ForeColor = SystemColors.ControlText;
        ClearSourceLink();
        ClearDestinationLink();
        downloadProgressBar.Value = 0;
        convertProgressBar.Value = 0;
        playlistProgressBar.Value = 0;
        playlistProgressLabel.Text = "";
        playlistProgressLabel.Visible = false;
        // Hide all progress bars
        downloadProgressBar.Visible = false;
        downloadLabel.Visible = false;
        convertProgressBar.Visible = false;
        convertLabel.Visible = false;
        playlistProgressBar.Visible = false;
        playlistLabel.Visible = false;
    }

    private void ClearInputFields()
    {
        urlTextBox.Clear();
        singleFolderComboBox.Text = "";
        playlistUrlTextBox.Clear();
        playlistFolderComboBox.Text = "";
        selectedPlaylistItems = null;
        excludedPlaylistItems = null;
        trackEachSongCheckBox.Checked = false;
    }

    private void RestartApplication()
    {
        try
        {
            // Get the path to the current executable
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                MessageBox.Show(
                    "Unable to determine application path for restart.\nPlease restart the application manually.",
                    "Restart Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Application.Exit();
                return;
            }

            // Start a new instance
            Process.Start(exePath);

            // Exit the current instance
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to restart application:\n{ex.Message}\n\nPlease restart manually.",
                "Restart Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private string SanitizeFolderName(string name)
    {
        // Remove invalid characters for folder names
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            name = name.Replace(c.ToString(), "");
        }
        // Trim and limit length
        name = name.Trim();
        if (name.Length > 100) name = name.Substring(0, 100);
        return name;
    }

    private string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        using var form = new Form
        {
            Text = title,
            Size = new Size(400, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = prompt,
            Location = new Point(15, 15),
            AutoSize = true
        };

        var textBox = new TextBox
        {
            Location = new Point(15, 40),
            Size = new Size(350, 23),
            Text = defaultValue
        };
        textBox.SelectAll();

        var okButton = new Button
        {
            Text = "OK",
            Location = new Point(200, 75),
            Size = new Size(75, 25),
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(285, 75),
            Size = new Size(75, 25),
            DialogResult = DialogResult.Cancel
        };

        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        return form.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
    }
}
