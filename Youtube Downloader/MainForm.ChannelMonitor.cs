using System.Diagnostics;

namespace Youtube_Downloader;

/// <summary>
/// Partial class containing channel monitoring methods for MainForm.
/// </summary>
public partial class MainForm
{
    private bool _isScanning = false;

    private void InitializeChannelMonitor()
    {
        channelMonitorStorage = new ChannelMonitorStorage();

        // Start the idle scan timer using config interval (only if enabled and there are channels)
        channelScanTimer = new System.Windows.Forms.Timer();
        channelScanTimer.Interval = config.ChannelScanIntervalMinutes * 60 * 1000;
        channelScanTimer.Tick += ChannelScanTimer_Tick;

        if (config.ChannelAutoScanEnabled && channelMonitorStorage.Channels.Count > 0)
        {
            channelScanTimer.Start();
            lastChannelScanTime = DateTime.Now;
            logger.Log($"Channel monitor auto-scan timer started ({config.ChannelScanIntervalMinutes} minute interval)");
        }
        else
        {
            logger.Log(config.ChannelAutoScanEnabled
                ? "Channel monitor auto-scan skipped (no channels to monitor)"
                : "Channel monitor auto-scan is disabled");
        }

        // Check for new videos and show alert if any
        UpdateChannelAlertPanel();
    }

    /// <summary>
    /// Gets the number of seconds until the next channel scan.
    /// Returns -1 if auto-scan is disabled or timer is not running.
    /// </summary>
    public int GetSecondsUntilNextScan()
    {
        if (!config.ChannelAutoScanEnabled || channelScanTimer == null || !channelScanTimer.Enabled || lastChannelScanTime == null)
            return -1;

        var elapsed = DateTime.Now - lastChannelScanTime.Value;
        var intervalSeconds = config.ChannelScanIntervalMinutes * 60;
        var remaining = intervalSeconds - (int)elapsed.TotalSeconds;
        return Math.Max(0, remaining);
    }

    /// <summary>
    /// Ensures the channel scan timer is running (called when first channel is added).
    /// </summary>
    public void EnsureChannelScanTimerRunning()
    {
        if (channelScanTimer == null || !config.ChannelAutoScanEnabled) return;

        if (!channelScanTimer.Enabled)
        {
            channelScanTimer.Start();
            lastChannelScanTime = DateTime.Now;
            logger.Log($"Channel monitor auto-scan timer started ({config.ChannelScanIntervalMinutes} minute interval)");
        }
    }

    private async void ChannelScanTimer_Tick(object? sender, EventArgs e)
    {
        // Only scan if enabled, not currently downloading, and not already scanning
        if (!config.ChannelAutoScanEnabled || _isScanning || isDownloading) return;

        try
        {
            _isScanning = true;
            logger.Log("Starting channel auto-scan...");
            await ScanChannelsAsync();
        }
        finally
        {
            _isScanning = false;
            // Reset the scan time for countdown calculation
            lastChannelScanTime = DateTime.Now;

            // Notify open Channel Monitor form to reset its countdown display
            if (openChannelMonitor != null && !openChannelMonitor.IsDisposed)
            {
                openChannelMonitor.ResetCountdownTimer();
            }
        }
    }

    private async Task ScanChannelsAsync()
    {
        if (channelMonitorStorage == null) return;

        try
        {
            // Create a snapshot of the channels list to avoid collection modified exception
            var channelsSnapshot = channelMonitorStorage.Channels.ToList();

            foreach (var channel in channelsSnapshot)
            {
                await FetchChannelVideosForIdleScanAsync(channel);
            }

            // Update the alert panel
            UpdateChannelAlertPanel();
        }
        catch (Exception ex)
        {
            logger.Log($"Error during channel scan: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch new videos for idle scan. Only checks for NEW videos (not already tracked).
    /// Uses flat-playlist for speed, then updates dates via RSS.
    /// </summary>
    private async Task FetchChannelVideosForIdleScanAsync(MonitoredChannel channel)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
            return;

        string cookiesArg = GetCookiesArgument();
        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtDlpPath,
            // Check first 20 videos to find any new ones (YouTube returns newest first)
            Arguments = $"{cookiesArg}--dump-json --flat-playlist --playlist-end 20 \"{channel.ChannelUrl}/videos\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var existingVideoIds = channel.Videos.Select(v => v.VideoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newVideos = new List<ChannelVideo>();
            bool foundExisting = false;

            while (!process.StandardOutput.EndOfStream)
            {
                string? line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    string videoId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(videoId)) continue;

                    // If we hit an existing video, we've found all new ones
                    if (existingVideoIds.Contains(videoId))
                    {
                        if (!foundExisting)
                        {
                            foundExisting = true;
                            Debug.WriteLine($"[IdleScan] Found existing video for {channel.ChannelName}, stopping");
                        }
                        continue;
                    }

                    // Check if already in download history
                    bool isInHistory = channelMonitorStorage?.IsVideoInDownloadHistory(videoId, history) ?? false;

                    var video = new ChannelVideo
                    {
                        VideoId = videoId,
                        Title = root.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                        ThumbnailUrl = root.TryGetProperty("thumbnail", out var thumb) ? thumb.GetString() ?? "" : "",
                        IsNew = !isInHistory,
                        Status = isInHistory ? VideoStatus.Downloaded : VideoStatus.New,
                        UploadDate = DateTime.MinValue  // Will be updated via RSS
                    };

                    if (root.TryGetProperty("duration", out var duration) && duration.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        video.Duration = TimeSpan.FromSeconds(duration.GetDouble());
                    }

                    newVideos.Add(video);
                    existingVideoIds.Add(videoId);
                }
                catch
                {
                    // Skip malformed JSON
                }
            }

            await process.WaitForExitAsync();

            // Insert new videos at the BEGINNING (they are newest)
            if (newVideos.Count > 0)
            {
                channel.Videos.InsertRange(0, newVideos);
                logger.Log($"Found {newVideos.Count} new video(s) from {channel.ChannelName}");

                // Update dates via RSS feed
                await UpdateVideoDatesFromRssForIdleScanAsync(channel);
            }

            channel.LastChecked = DateTime.Now;
            channelMonitorStorage?.Save();
        }
        catch (Exception ex)
        {
            logger.Log($"Error fetching videos for {channel.ChannelName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch YouTube RSS feed and update upload dates for matching videos.
    /// </summary>
    private async Task UpdateVideoDatesFromRssForIdleScanAsync(MonitoredChannel channel)
    {
        if (string.IsNullOrEmpty(channel.ChannelId))
            return;

        string rssUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={channel.ChannelId}";

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            string rssContent = await httpClient.GetStringAsync(rssUrl);

            var xdoc = System.Xml.Linq.XDocument.Parse(rssContent);
            System.Xml.Linq.XNamespace atom = "http://www.w3.org/2005/Atom";
            System.Xml.Linq.XNamespace yt = "http://www.youtube.com/xml/schemas/2015";

            var entries = xdoc.Descendants(atom + "entry");

            foreach (var entry in entries)
            {
                string? videoId = entry.Element(yt + "videoId")?.Value;
                if (string.IsNullOrEmpty(videoId)) continue;

                var video = channel.Videos.FirstOrDefault(v =>
                    v.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));

                if (video != null && video.UploadDate == DateTime.MinValue)
                {
                    string? publishedStr = entry.Element(atom + "published")?.Value;
                    if (!string.IsNullOrEmpty(publishedStr) && DateTime.TryParse(publishedStr, out var publishedDate))
                    {
                        video.UploadDate = publishedDate.Date;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IdleScan] Error fetching RSS for {channel.ChannelName}: {ex.Message}");
            // RSS fetch is optional - continue without dates
        }
    }

    // Track whether alert panel offset is currently applied
    private bool _alertOffsetApplied = false;
    private const int AlertPanelOffset = 33;  // Height of alert panel + spacing

    private void UpdateChannelAlertPanel()
    {
        if (channelMonitorStorage == null) return;

        int newVideoCount = channelMonitorStorage.GetNewVideoCount();

        if (newVideoCount > 0)
        {
            // Create alert panel if it doesn't exist
            if (channelAlertPanel == null)
            {
                channelAlertPanel = new Panel
                {
                    BackColor = Color.FromArgb(255, 200, 100),  // Orange-ish
                    Height = 28,
                    Cursor = Cursors.Hand,
                    // Position below menu strip, not docked to avoid covering tabs
                    Location = new Point(0, menuStrip.Bottom),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                channelAlertPanel.Click += ChannelAlertPanel_Click;

                channelAlertLabel = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
                };
                channelAlertLabel.Click += ChannelAlertPanel_Click;
                channelAlertPanel.Controls.Add(channelAlertLabel);

                Controls.Add(channelAlertPanel);
                channelAlertPanel.BringToFront();
            }

            // Update width to match form
            channelAlertPanel.Width = ClientSize.Width;
            channelAlertLabel!.Text = $"{newVideoCount} new video(s) from monitored channels - Click to view";
            channelAlertPanel.Visible = true;

            // Move all controls down to make room for alert if not already done
            if (!_alertOffsetApplied)
            {
                _alertOffsetApplied = true;
                // Increase form height
                Height += AlertPanelOffset;
                MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height + AlertPanelOffset);

                // Move all controls below menu strip down
                downloadTabControl.Top += AlertPanelOffset;
                downloadLabel.Top += AlertPanelOffset;
                downloadProgressBar.Top += AlertPanelOffset;
                convertLabel.Top += AlertPanelOffset;
                convertProgressBar.Top += AlertPanelOffset;
                playlistLabel.Top += AlertPanelOffset;
                playlistProgressBar.Top += AlertPanelOffset;
                playlistProgressLabel.Top += AlertPanelOffset;
                statusLabel.Top += AlertPanelOffset;
                sourceLink.Top += AlertPanelOffset;
                destinationLink.Top += AlertPanelOffset;
                cancelButton.Top += AlertPanelOffset;
                clearAllButton.Top += AlertPanelOffset;
            }
        }
        else
        {
            // Hide alert panel
            if (channelAlertPanel != null)
            {
                channelAlertPanel.Visible = false;
            }

            // Restore all controls to original positions if offset was applied
            if (_alertOffsetApplied)
            {
                _alertOffsetApplied = false;
                // Restore form height
                Height -= AlertPanelOffset;
                MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height - AlertPanelOffset);

                // Move all controls back up
                downloadTabControl.Top -= AlertPanelOffset;
                downloadLabel.Top -= AlertPanelOffset;
                downloadProgressBar.Top -= AlertPanelOffset;
                convertLabel.Top -= AlertPanelOffset;
                convertProgressBar.Top -= AlertPanelOffset;
                playlistLabel.Top -= AlertPanelOffset;
                playlistProgressBar.Top -= AlertPanelOffset;
                playlistProgressLabel.Top -= AlertPanelOffset;
                statusLabel.Top -= AlertPanelOffset;
                sourceLink.Top -= AlertPanelOffset;
                destinationLink.Top -= AlertPanelOffset;
                cancelButton.Top -= AlertPanelOffset;
                clearAllButton.Top -= AlertPanelOffset;
            }
        }
    }

    private void ChannelAlertPanel_Click(object? sender, EventArgs e)
    {
        // Open Channel Monitor form
        OpenChannelMonitorForm();
    }

    private void ChannelsMenuItem_Click(object? sender, EventArgs e)
    {
        OpenChannelMonitorForm();
    }

    private void OpenChannelMonitorForm()
    {
        if (channelMonitorStorage == null)
        {
            channelMonitorStorage = new ChannelMonitorStorage();
        }

        // Hide the alert panel when opening the monitor form
        if (channelAlertPanel != null)
        {
            channelAlertPanel.Visible = false;
        }

        // Reuse existing form if still open
        if (openChannelMonitor != null && !openChannelMonitor.IsDisposed)
        {
            openChannelMonitor.BringToFront();
            openChannelMonitor.Focus();
            return;
        }

        openChannelMonitor = new ChannelMonitorForm(channelMonitorStorage, config, this, history);
        openChannelMonitor.FormClosed += (s, e) =>
        {
            openChannelMonitor = null;
            UpdateChannelAlertPanel();
        };
        openChannelMonitor.Show(this);
    }

    /// <summary>
    /// Queue a video for download from the Channel Monitor
    /// Fills the URL field and automatically starts the download
    /// </summary>
    public void QueueDownloadFromMonitor(string videoUrl, string videoTitle)
    {
        // Switch to single video tab
        downloadTabControl.SelectedIndex = 0;

        // Set the URL
        urlTextBox.Text = videoUrl;
        statusLabel.Text = $"Starting download: {videoTitle}";

        // Automatically trigger the download by clicking the Go button
        goButton.PerformClick();
    }

    /// <summary>
    /// Check if a channel is already being monitored
    /// </summary>
    public bool IsChannelMonitored(string channelId)
    {
        if (string.IsNullOrEmpty(channelId)) return false;
        if (channelMonitorStorage == null)
        {
            channelMonitorStorage = new ChannelMonitorStorage();
        }
        return channelMonitorStorage.HasChannel(channelId);
    }

    /// <summary>
    /// Add a channel to monitor from History
    /// </summary>
    public async Task AddChannelToMonitorAsync(string channelId, string channelName, string channelUrl)
    {
        if (channelMonitorStorage == null)
        {
            channelMonitorStorage = new ChannelMonitorStorage();
        }

        if (channelMonitorStorage.HasChannel(channelId))
        {
            MessageBox.Show($"Channel '{channelName}' is already being monitored.",
                "Already Monitored", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Open Channel Monitor form and add the channel
        OpenChannelMonitorForm();

        // Wait a moment for the form to be ready
        await Task.Delay(100);

        if (openChannelMonitor != null && !openChannelMonitor.IsDisposed)
        {
            await openChannelMonitor.AddChannelAsync(channelUrl);
        }
    }
}
