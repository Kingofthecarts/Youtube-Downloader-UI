using System.Diagnostics;

namespace Youtube_Downloader;

public class ChannelMonitorForm : Form
{
    private readonly ChannelMonitorStorage storage;
    private readonly Config config;
    private readonly MainForm mainForm;
    private readonly DownloadHistory downloadHistory;

    private ListBox channelListBox = null!;
    private ContextMenuStrip channelContextMenu = null!;
    private SafePictureBox channelBanner = null!;
    private Label channelNameLabel = null!;
    private Label lastCheckedLabel = null!;
    private ComboBox statusFilterComboBox = null!;
    private TextBox searchFilterTextBox = null!;
    private CheckBox showAllVideosCheckBox = null!;
    private DataGridView videosGrid = null!;
    private Label videoStatsLabel = null!;
    private Button addChannelButton = null!;
    private Button removeChannelButton = null!;
    private Button resetChannelButton = null!;
    private Button snoozeAllButton = null!;
    private Button refreshButton = null!;
    private Button refreshAllButton = null!;
    private Button cancelFetchButton = null!;
    private Label statusLabel = null!;
    private CancellationTokenSource? fetchCancellation;
    private Label nextScanLabel = null!;
    private System.Windows.Forms.Timer countdownTimer = null!;

    private MonitoredChannel? selectedChannel;
    private int secondsUntilNextScan;

    public ChannelMonitorForm(ChannelMonitorStorage storage, Config config, MainForm mainForm, DownloadHistory downloadHistory)
    {
        this.storage = storage;
        this.config = config;
        this.mainForm = mainForm;
        this.downloadHistory = downloadHistory;
        InitializeComponents();
        LoadChannels();
        InitializeCountdownTimer();
    }

    private void InitializeComponents()
    {
        Text = "Channel Monitor";
        Size = new Size(1150, 780);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(950, 600);
        MaximizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Left panel - Channel list
        var channelListLabel = new Label
        {
            Text = "Monitored Channels",
            Location = new Point(12, 12),
            Size = new Size(200, 20),
            Font = new Font(Font, FontStyle.Bold)
        };

        channelListBox = new ListBox
        {
            Location = new Point(12, 35),
            Size = new Size(200, 350),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };
        channelListBox.SelectedIndexChanged += ChannelListBox_SelectedIndexChanged;

        // Context menu for channel list
        channelContextMenu = new ContextMenuStrip();
        var refreshBannerItem = new ToolStripMenuItem("Refresh Banner");
        refreshBannerItem.Click += RefreshBannerMenuItem_Click;
        var openChannelItem = new ToolStripMenuItem("Open Channel in Browser");
        openChannelItem.Click += OpenChannelMenuItem_Click;
        channelContextMenu.Items.Add(refreshBannerItem);
        channelContextMenu.Items.Add(openChannelItem);
        channelListBox.ContextMenuStrip = channelContextMenu;

        // Left panel buttons - Row 1: Add Channel and Refresh All
        addChannelButton = new Button
        {
            Text = "Add Channel...",
            Location = new Point(12, 390),
            Size = new Size(95, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        addChannelButton.Click += AddChannelButton_Click;

        refreshAllButton = new Button
        {
            Text = "Refresh All",
            Location = new Point(112, 390),
            Size = new Size(100, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        refreshAllButton.Click += RefreshAllButton_Click;

        // Left panel buttons - Row 2: Remove
        removeChannelButton = new Button
        {
            Text = "Remove",
            Location = new Point(12, 422),
            Size = new Size(95, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Enabled = false
        };
        removeChannelButton.Click += RemoveChannelButton_Click;

        // Right panel - Channel details and videos
        channelBanner = new SafePictureBox
        {
            Location = new Point(225, 12),
            Size = new Size(750, 100),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        channelNameLabel = new Label
        {
            Text = "Select a channel",
            Location = new Point(225, 118),
            AutoSize = true,  // Auto-size to fit text
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Cursor = Cursors.Default,  // Default cursor until channel selected
            ForeColor = Color.Black  // Black until channel selected
        };
        channelNameLabel.Click += ChannelNameLabel_Click;

        lastCheckedLabel = new Label
        {
            Text = "",
            Location = new Point(225, 143),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        // Status filter dropdown
        var statusFilterLabel = new Label
        {
            Text = "Filter:",
            Location = new Point(430, 144),
            Size = new Size(40, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Visible = false
        };
        Controls.Add(statusFilterLabel);

        statusFilterComboBox = new ComboBox
        {
            Location = new Point(470, 140),
            Size = new Size(100, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Enabled = false,
            Visible = false
        };
        statusFilterComboBox.Items.AddRange(new object[] { "All", "New", "Snoozed", "Watched", "Downloaded", "Ignored" });
        statusFilterComboBox.SelectedIndex = 0;
        statusFilterComboBox.SelectedIndexChanged += StatusFilterComboBox_SelectedIndexChanged;
        // Store reference to label for visibility toggling
        statusFilterComboBox.Tag = statusFilterLabel;

        showAllVideosCheckBox = new CheckBox
        {
            Text = "Show all",
            Location = new Point(580, 142),
            Size = new Size(80, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Enabled = false,
            Visible = false  // Hidden until channel selected
        };
        showAllVideosCheckBox.CheckedChanged += ShowAllVideosCheckBox_CheckedChanged;

        // Search filter textbox - above the grid
        searchFilterTextBox = new TextBox
        {
            Location = new Point(225, 170),
            Size = new Size(250, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            PlaceholderText = "Search videos...",
            Enabled = false,
            Visible = false  // Hidden until channel selected
        };
        searchFilterTextBox.TextChanged += SearchFilterTextBox_TextChanged;

        // Right panel buttons - Snooze All, Reset and Refresh Channel
        snoozeAllButton = new Button
        {
            Text = "Snooze All",
            Location = new Point(780, 118),
            Size = new Size(75, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false,
            Visible = false  // Hidden until channel selected
        };
        snoozeAllButton.Click += SnoozeAllButton_Click;

        resetChannelButton = new Button
        {
            Text = "Reset",
            Location = new Point(860, 118),
            Size = new Size(60, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false,
            Visible = false  // Hidden until channel selected
        };
        resetChannelButton.Click += ResetChannelButton_Click;

        refreshButton = new Button
        {
            Text = "Refresh",
            Location = new Point(925, 118),
            Size = new Size(70, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false,
            Visible = false  // Hidden until channel selected
        };
        refreshButton.Click += RefreshButton_Click;

        cancelFetchButton = new Button
        {
            Text = "Cancel",
            Location = new Point(520, 705),
            Size = new Size(70, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Visible = false,  // Only visible during fetch
            BackColor = Color.FromArgb(255, 200, 200)
        };
        cancelFetchButton.Click += CancelFetchButton_Click;

        // Videos grid - below search bar
        videosGrid = new DataGridView
        {
            Location = new Point(225, 200),
            Size = new Size(900, 380),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(51, 153, 255),
                SelectionForeColor = Color.White
            }
        };

        // Video columns
        videosGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNum", HeaderText = "#", Width = 35, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", FillWeight = 50 });
        videosGrid.Columns.Add(new DataGridViewLinkColumn
        {
            Name = "VideoId",
            HeaderText = "Video ID",
            Width = 100,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            LinkBehavior = LinkBehavior.HoverUnderline,
            TrackVisitedState = false
        });
        videosGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UploadDate", HeaderText = "Upload Date", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Duration", Width = 70, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Download", HeaderText = "", Text = "Download", UseColumnTextForButtonValue = true, Width = 75, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Snooze", HeaderText = "", Width = 65, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        videosGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Ignore", HeaderText = "", Width = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

        videosGrid.CellContentClick += VideosGrid_CellContentClick;
        videosGrid.CellDoubleClick += VideosGrid_CellDoubleClick;

        // Video stats label (above status bar)
        videoStatsLabel = new Label
        {
            Text = "",
            Location = new Point(225, 590),
            Size = new Size(500, 20),
            ForeColor = Color.DarkBlue,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Status bar - near bottom
        statusLabel = new Label
        {
            Text = "",
            Location = new Point(12, 695),
            Size = new Size(500, 20),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Next scan countdown label - under status
        nextScanLabel = new Label
        {
            Text = "Next scan in: --:--",
            Location = new Point(12, 715),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Close button
        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(1045, 705),
            Size = new Size(80, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        closeButton.Click += (s, e) => Close();

        Controls.Add(channelListLabel);
        Controls.Add(channelListBox);
        Controls.Add(addChannelButton);
        Controls.Add(refreshAllButton);
        Controls.Add(removeChannelButton);
        Controls.Add(channelBanner);
        Controls.Add(channelNameLabel);
        Controls.Add(lastCheckedLabel);
        Controls.Add(statusFilterComboBox);
        Controls.Add(showAllVideosCheckBox);
        Controls.Add(searchFilterTextBox);
        Controls.Add(snoozeAllButton);
        Controls.Add(resetChannelButton);
        Controls.Add(refreshButton);
        Controls.Add(cancelFetchButton);
        Controls.Add(videosGrid);
        Controls.Add(videoStatsLabel);
        Controls.Add(statusLabel);
        Controls.Add(nextScanLabel);
        Controls.Add(closeButton);
    }

    private void LoadChannels()
    {
        channelListBox.Items.Clear();

        // Create a snapshot to avoid collection modified exception
        var channelsSnapshot = storage.Channels.ToList();

        foreach (var channel in channelsSnapshot)
        {
            int newCount = channel.Videos.Count(v => v.Status == VideoStatus.New);
            string displayText = newCount > 0
                ? $"{channel.ChannelName} ({newCount} new)"
                : channel.ChannelName;
            channelListBox.Items.Add(new ChannelListItem(channel, displayText));
        }

        UpdateStatusLabel();
    }

    private void InitializeCountdownTimer()
    {
        // Get remaining time from MainForm's timer (syncs with app-level timer)
        int remainingSeconds = mainForm.GetSecondsUntilNextScan();
        if (remainingSeconds >= 0)
        {
            secondsUntilNextScan = remainingSeconds;
        }
        else
        {
            // Fallback to full interval if timer not running
            secondsUntilNextScan = config.ChannelScanIntervalMinutes * 60;
        }
        UpdateNextScanLabel();

        countdownTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000 // 1 second
        };
        countdownTimer.Tick += CountdownTimer_Tick;

        // Only start timer if auto-scan is enabled
        if (config.ChannelAutoScanEnabled)
        {
            countdownTimer.Start();
        }
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        if (secondsUntilNextScan > 0)
        {
            secondsUntilNextScan--;
            UpdateNextScanLabel();
        }
        else
        {
            // Timer reached zero, reset to full interval
            ResetCountdown();
        }
    }

    private void UpdateNextScanLabel()
    {
        if (!config.ChannelAutoScanEnabled)
        {
            nextScanLabel.Text = "Auto-scan disabled";
            return;
        }

        int minutes = secondsUntilNextScan / 60;
        int seconds = secondsUntilNextScan % 60;
        nextScanLabel.Text = $"Next scan in: {minutes}:{seconds:D2}";
    }

    private void ResetCountdown()
    {
        secondsUntilNextScan = config.ChannelScanIntervalMinutes * 60;
        UpdateNextScanLabel();
    }

    /// <summary>
    /// Public method to reset the countdown timer (called from MainForm after auto-scan)
    /// </summary>
    public void ResetCountdownTimer()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(ResetCountdown));
        }
        else
        {
            ResetCountdown();
        }
    }

    private void ChannelListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (channelListBox.SelectedItem is ChannelListItem item)
        {
            selectedChannel = item.Channel;
            DisplayChannel(selectedChannel);

            // Show and enable controls when channel is selected
            removeChannelButton.Enabled = true;
            snoozeAllButton.Enabled = true;
            snoozeAllButton.Visible = true;
            resetChannelButton.Enabled = true;
            resetChannelButton.Visible = true;
            refreshButton.Enabled = true;
            refreshButton.Visible = true;
            statusFilterComboBox.Enabled = true;
            statusFilterComboBox.Visible = true;
            if (statusFilterComboBox.Tag is Label filterLabel)
                filterLabel.Visible = true;
            showAllVideosCheckBox.Enabled = true;
            showAllVideosCheckBox.Visible = true;
            searchFilterTextBox.Enabled = true;
            searchFilterTextBox.Visible = true;
        }
        else
        {
            selectedChannel = null;
            ClearChannelDisplay();

            // Hide and disable controls when no channel selected
            removeChannelButton.Enabled = false;
            snoozeAllButton.Enabled = false;
            snoozeAllButton.Visible = false;
            resetChannelButton.Enabled = false;
            resetChannelButton.Visible = false;
            refreshButton.Enabled = false;
            refreshButton.Visible = false;
            statusFilterComboBox.Enabled = false;
            statusFilterComboBox.Visible = false;
            if (statusFilterComboBox.Tag is Label filterLabel)
                filterLabel.Visible = false;
            showAllVideosCheckBox.Enabled = false;
            showAllVideosCheckBox.Visible = false;
            searchFilterTextBox.Enabled = false;
            searchFilterTextBox.Visible = false;
            searchFilterTextBox.Text = "";  // Clear search when deselecting channel
            videoStatsLabel.Text = "";
        }
    }

    private void DisplayChannel(MonitoredChannel channel)
    {
        channelNameLabel.Text = channel.ChannelName;
        channelNameLabel.ForeColor = Color.Blue;
        channelNameLabel.Cursor = Cursors.Hand;

        if (channel.LastChecked == DateTime.MinValue)
        {
            lastCheckedLabel.Text = "Never checked";
        }
        else
        {
            lastCheckedLabel.Text = $"Last checked: {channel.LastChecked:g}";
        }

        // Load banner
        LoadChannelBanner(channel);

        // Display videos
        DisplayVideos(channel);
    }

    private void LoadChannelBanner(MonitoredChannel channel)
    {
        channelBanner.Image?.Dispose();
        channelBanner.Image = null;

        if (!string.IsNullOrEmpty(channel.BannerPath) && File.Exists(channel.BannerPath))
        {
            try
            {
                // Read file into memory to avoid file locking issues
                // Create a copy of the image so we don't need to keep the stream alive
                byte[] imageData = File.ReadAllBytes(channel.BannerPath);
                using var ms = new MemoryStream(imageData);
                using var tempImage = Image.FromStream(ms);
                // Create a copy that doesn't depend on the stream
                channelBanner.Image = new Bitmap(tempImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChannelMonitor] Error loading banner: {ex.Message}");
                // If we can't load the banner, leave it blank
            }
        }
    }

    private void DisplayVideos(MonitoredChannel channel)
    {
        videosGrid.Rows.Clear();

        // Start with all videos
        var filteredVideos = channel.Videos.AsEnumerable();

        // Apply status filter from dropdown
        string selectedFilter = statusFilterComboBox.SelectedItem?.ToString() ?? "All";
        if (selectedFilter != "All")
        {
            if (Enum.TryParse<VideoStatus>(selectedFilter, out var filterStatus))
            {
                filteredVideos = filteredVideos.Where(v => v.Status == filterStatus);
            }
        }
        else if (!showAllVideosCheckBox.Checked)
        {
            // Default: hide Ignored and Downloaded unless "Show all" is checked
            filteredVideos = filteredVideos.Where(v =>
                v.Status != VideoStatus.Ignored && v.Status != VideoStatus.Downloaded);
        }

        // Apply search filter
        string searchText = searchFilterTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredVideos = filteredVideos.Where(v =>
                v.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                v.VideoId.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Keep videos in original order (YouTube returns newest first)
        // Don't re-sort - preserve the fetch order which represents recency
        var sortedVideos = filteredVideos.ToList();

        // Apply MaxChannelVideosShown limit if configured (0 = unlimited)
        int maxVideos = config.MaxChannelVideosShown;
        if (maxVideos > 0 && sortedVideos.Count > maxVideos)
        {
            sortedVideos = sortedVideos.Take(maxVideos).ToList();
        }

        int rowNum = 1;
        foreach (var video in sortedVideos)
        {
            int rowIndex = videosGrid.Rows.Add();
            var row = videosGrid.Rows[rowIndex];

            row.Cells["RowNum"].Value = rowNum++;
            row.Cells["Title"].Value = video.Title;
            row.Cells["VideoId"].Value = video.VideoId;
            row.Cells["UploadDate"].Value = video.UploadDate == DateTime.MinValue
                ? "N/A"
                : video.UploadDate.ToString("yyyy-MM-dd");
            row.Cells["Duration"].Value = FormatDuration(video.Duration);
            row.Cells["Status"].Value = video.Status.ToString();

            row.Tag = video;

            // Highlight based on status and configure buttons
            if (video.Status == VideoStatus.New)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);  // Light yellow
                // All buttons enabled for New status
                row.Cells["Snooze"].Value = "Snooze";
                row.Cells["Ignore"].Value = "Ignore";
            }
            else if (video.Status == VideoStatus.Downloaded)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 200);  // Light green
                // Change Download button to "Play"
                row.Cells["Download"].Value = "Play";
                // Disable Snooze and Ignore buttons - not applicable for downloaded
                row.Cells["Snooze"].Value = "";
                row.Cells["Ignore"].Value = "";
            }
            else if (video.Status == VideoStatus.Watched)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);  // Light blue
                // Show Unsnooze to go back to New
                row.Cells["Snooze"].Value = "Unsnooze";
                row.Cells["Ignore"].Value = "Ignore";
            }
            else if (video.Status == VideoStatus.Snoozed)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);  // Light gray
                // Show Unsnooze button to go back to New
                row.Cells["Snooze"].Value = "Unsnooze";
                row.Cells["Ignore"].Value = "Ignore";
            }
            else if (video.Status == VideoStatus.Ignored)
            {
                row.DefaultCellStyle.ForeColor = Color.Gray;
                // Show "Wake" instead of "Ignore" button, show Unsnooze
                row.Cells["Snooze"].Value = "Unsnooze";
                row.Cells["Ignore"].Value = "Wake";
            }
        }

        // Update video stats label
        UpdateStatsLabel(channel);
    }

    private void ClearChannelDisplay()
    {
        channelNameLabel.Text = "Select a channel";
        channelNameLabel.ForeColor = Color.Black;
        channelNameLabel.Cursor = Cursors.Default;
        lastCheckedLabel.Text = "";
        channelBanner.Image?.Dispose();
        channelBanner.Image = null;
        videosGrid.Rows.Clear();
    }

    private void UpdateStatsLabel(MonitoredChannel channel)
    {
        int newCount = channel.Videos.Count(v => v.Status == VideoStatus.New);
        int snoozedCount = channel.Videos.Count(v => v.Status == VideoStatus.Snoozed);
        int downloadedCount = channel.Videos.Count(v => v.Status == VideoStatus.Downloaded);
        int totalCount = channel.Videos.Count;
        int ignoredCount = channel.Videos.Count(v => v.Status == VideoStatus.Ignored);

        string statsText = $"New: {newCount} | Snoozed: {snoozedCount} | Downloaded: {downloadedCount} | Total: {totalCount} | Ignored: {ignoredCount}";

        // Add note if max videos limit is applied
        int maxVideos = config.MaxChannelVideosShown;
        if (maxVideos > 0 && totalCount > maxVideos)
        {
            statsText += $" (showing {maxVideos})";
        }

        videoStatsLabel.Text = statsText;
    }

    private void UpdateStatusLabel()
    {
        int totalChannels = storage.Channels.Count;
        int newVideos = storage.GetNewVideoCount();

        statusLabel.Text = $"{totalChannels} channel(s) monitored, {newVideos} new video(s)";
    }

    private string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private async void AddChannelButton_Click(object? sender, EventArgs e)
    {
        using var inputForm = new AddChannelForm();
        if (inputForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(inputForm.ChannelUrl))
        {
            await AddChannelAsync(inputForm.ChannelUrl);
        }
    }

    public async Task AddChannelAsync(string channelUrl)
    {
        statusLabel.Text = $"Fetching channel info from {channelUrl}...";
        addChannelButton.Enabled = false;
        refreshAllButton.Enabled = false;
        cancelFetchButton.Visible = true;

        // Create cancellation token for this operation
        fetchCancellation?.Dispose();
        fetchCancellation = new CancellationTokenSource();

        try
        {
            // Use yt-dlp to get channel info
            var channelInfo = await GetChannelInfoAsync(channelUrl);

            if (fetchCancellation.Token.IsCancellationRequested)
            {
                statusLabel.Text = "Add channel cancelled.";
                return;
            }

            if (channelInfo == null)
            {
                MessageBox.Show("Could not retrieve channel information. Please check the URL.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error: Could not retrieve channel information";
                return;
            }

            if (storage.HasChannel(channelInfo.ChannelId))
            {
                MessageBox.Show($"Channel '{channelInfo.ChannelName}' is already being monitored.",
                    "Already Monitored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = $"Channel '{channelInfo.ChannelName}' is already being monitored";
                return;
            }

            // Add channel immediately to storage and list (before fetching videos)
            storage.AddChannel(channelInfo);
            LoadChannels();

            // Ensure the MainForm scan timer is running (starts if this is the first channel)
            mainForm.EnsureChannelScanTimerRunning();

            // Select the newly added channel immediately
            for (int i = 0; i < channelListBox.Items.Count; i++)
            {
                if (channelListBox.Items[i] is ChannelListItem item &&
                    item.Channel.ChannelId == channelInfo.ChannelId)
                {
                    channelListBox.SelectedIndex = i;
                    break;
                }
            }

            statusLabel.Text = $"Channel added: {channelInfo.ChannelName}. Fetching videos...";

            // Download banner in background
            statusLabel.Text = $"Downloading channel banner for {channelInfo.ChannelName}...";
            await DownloadChannelBannerAsync(channelInfo);

            // Reload banner if this channel is still selected
            if (selectedChannel?.ChannelId == channelInfo.ChannelId)
            {
                LoadChannelBanner(channelInfo);
            }

            if (fetchCancellation.Token.IsCancellationRequested)
            {
                statusLabel.Text = "Add channel cancelled.";
                return;
            }

            // Fetch videos with real-time grid updates
            statusLabel.Text = $"Fetching videos from {channelInfo.ChannelName}...";
            await FetchChannelVideosWithProgressAsync(channelInfo, fetchCancellation.Token);

            // Save after fetching videos
            storage.UpdateChannel(channelInfo);

            // Refresh display
            if (selectedChannel?.ChannelId == channelInfo.ChannelId)
            {
                DisplayVideos(channelInfo);
            }

            int videoCount = channelInfo.Videos.Count;
            int newCount = channelInfo.Videos.Count(v => v.Status == VideoStatus.New);
            statusLabel.Text = $"Channel added: {channelInfo.ChannelName} ({newCount} new, {videoCount} total videos)";
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Add channel cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding channel: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            addChannelButton.Enabled = true;
            refreshAllButton.Enabled = true;
            cancelFetchButton.Visible = false;
        }
    }

    /// <summary>
    /// Normalizes a YouTube channel URL to a standard format.
    /// Supports: @username, /channel/UCxxx, /c/name, /user/name formats
    /// </summary>
    private string NormalizeChannelUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        url = url.Trim();

        // Ensure it starts with https://
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Check if it looks like a YouTube handle or channel reference
            if (url.StartsWith("@"))
            {
                url = $"https://www.youtube.com/{url}";
            }
            else if (url.StartsWith("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                     url.StartsWith("www.youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                url = $"https://{url}";
            }
        }

        // Convert http to https
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url.Substring(7);
        }

        // Remove trailing slashes and /videos, /about, etc. for cleaner URL
        url = url.TrimEnd('/');
        string[] suffixes = { "/videos", "/about", "/playlists", "/community", "/channels", "/featured" };
        foreach (var suffix in suffixes)
        {
            if (url.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(0, url.Length - suffix.Length);
                break;
            }
        }

        return url;
    }

    /// <summary>
    /// Checks if the URL is a valid YouTube channel URL format
    /// </summary>
    private bool IsValidChannelUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        string normalized = NormalizeChannelUrl(url).ToLowerInvariant();

        // Check for valid YouTube channel URL patterns
        return normalized.Contains("youtube.com/@") ||
               normalized.Contains("youtube.com/channel/") ||
               normalized.Contains("youtube.com/c/") ||
               normalized.Contains("youtube.com/user/");
    }

    private string GetCookiesArgument()
    {
        var cookiePath = YouTubeCookieManager.GetCookiesFilePath(config);
        return !string.IsNullOrEmpty(cookiePath) ? $"--cookies \"{cookiePath}\" " : "";
    }

    private async Task<MonitoredChannel?> GetChannelInfoAsync(string channelUrl)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
        {
            throw new Exception("yt-dlp is not configured. Please set it up in settings.");
        }

        // Normalize the URL before processing
        channelUrl = NormalizeChannelUrl(channelUrl);
        Debug.WriteLine($"[ChannelMonitor] Getting channel info for: {channelUrl}");

        string cookiesArg = GetCookiesArgument();
        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtDlpPath,
            Arguments = $"{cookiesArg}--dump-json --playlist-items 1 \"{channelUrl}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (string.IsNullOrEmpty(output))
        {
            Debug.WriteLine($"[ChannelMonitor] No output from yt-dlp. Error: {error}");
            return null;
        }

        // Parse JSON to extract channel info
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(output);
            var root = doc.RootElement;

            var channel = new MonitoredChannel
            {
                ChannelId = root.TryGetProperty("channel_id", out var chId) ? chId.GetString() ?? "" : "",
                ChannelName = root.TryGetProperty("channel", out var chName) ? chName.GetString() ?? "" : "",
                ChannelUrl = root.TryGetProperty("channel_url", out var chUrl) ? chUrl.GetString() ?? "" : channelUrl,
                DateAdded = DateTime.Now,
                MonitorFromDate = DateTime.Now.AddDays(-30)  // Default to 30 days ago for new channels
            };

            // Fallback for channel name
            if (string.IsNullOrEmpty(channel.ChannelName) && root.TryGetProperty("uploader", out var uploader))
            {
                channel.ChannelName = uploader.GetString() ?? "";
            }

            // Fallback for channel ID - try multiple fields
            if (string.IsNullOrEmpty(channel.ChannelId))
            {
                // Try uploader_id
                if (root.TryGetProperty("uploader_id", out var uploaderId))
                {
                    string? uploaderIdStr = uploaderId.GetString();
                    if (!string.IsNullOrEmpty(uploaderIdStr))
                    {
                        // uploader_id might be @handle format, try to get actual channel_id from channel_url
                        channel.ChannelId = uploaderIdStr;
                    }
                }

                // If channel_url contains channel ID, extract it
                if (!string.IsNullOrEmpty(channel.ChannelUrl) && channel.ChannelUrl.Contains("/channel/"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        channel.ChannelUrl, @"/channel/(UC[a-zA-Z0-9_-]+)");
                    if (match.Success)
                    {
                        channel.ChannelId = match.Groups[1].Value;
                    }
                }
            }

            Debug.WriteLine($"[ChannelMonitor] Parsed channel: {channel.ChannelName} (ID: {channel.ChannelId})");
            return channel;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Error parsing channel info: {ex.Message}");
            return null;
        }
    }

    private async Task DownloadChannelBannerAsync(MonitoredChannel channel, bool forceRefresh = false)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
            return;

        // Check if we should skip banner download (caching logic)
        if (!forceRefresh)
        {
            bool bannerExists = !string.IsNullOrEmpty(channel.BannerPath) && File.Exists(channel.BannerPath);
            bool recentlyUpdated = channel.BannerLastUpdated != DateTime.MinValue &&
                                   (DateTime.Now - channel.BannerLastUpdated).TotalDays < 7;

            if (bannerExists && recentlyUpdated)
            {
                Debug.WriteLine($"[ChannelMonitor] Skipping banner download for {channel.ChannelName} - cached and less than 7 days old");
                return;
            }
        }

        try
        {
            string? bannerUrl = null;

            // Method 1: Use yt-dlp --write-thumbnail to get channel avatar more reliably
            string tempDir = Path.Combine(Path.GetTempPath(), "ytdl_banner_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string cookiesArg = GetCookiesArgument();
                var startInfo = new ProcessStartInfo
                {
                    FileName = config.YtDlpPath,
                    Arguments = $"{cookiesArg}--write-thumbnail --skip-download --playlist-items 0 --convert-thumbnails jpg -o \"{tempDir}/avatar\" \"{channel.ChannelUrl}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();

                // Check if thumbnail was downloaded
                var thumbnailFiles = Directory.GetFiles(tempDir, "avatar*.*");
                if (thumbnailFiles.Length > 0)
                {
                    string srcFile = thumbnailFiles[0];
                    string extension = Path.GetExtension(srcFile);
                    string bannerPath = Path.Combine(
                        storage.GetBannerStoragePath(),
                        $"{channel.ChannelId}{extension}");

                    // Delete old banner if exists with different extension
                    if (!string.IsNullOrEmpty(channel.BannerPath) && File.Exists(channel.BannerPath) &&
                        !channel.BannerPath.Equals(bannerPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(channel.BannerPath); } catch { }
                    }

                    File.Copy(srcFile, bannerPath, true);
                    channel.BannerPath = bannerPath;
                    channel.BannerLastUpdated = DateTime.Now;
                    Debug.WriteLine($"[ChannelMonitor] Banner saved via yt-dlp to: {bannerPath}");
                    return;
                }
            }
            finally
            {
                // Clean up temp directory
                try { Directory.Delete(tempDir, true); } catch { }
            }

            // Method 2: Fallback - get full metadata from first video to extract channel avatar
            var videoStartInfo = new ProcessStartInfo
            {
                FileName = config.YtDlpPath,
                // Use full metadata (not flat-playlist) to get channel thumbnail info
                Arguments = $"--dump-json --playlist-items 1 \"{channel.ChannelUrl}/videos\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var videoProcess = new Process { StartInfo = videoStartInfo };
            videoProcess.Start();

            string output = await videoProcess.StandardOutput.ReadToEndAsync();
            await videoProcess.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;

                // Try channel avatar/thumbnail fields first (these are the actual channel avatar URLs)
                string[] avatarFields = { "channel_thumbnail_url", "uploader_thumbnail", "channel_thumbnail" };
                foreach (var field in avatarFields)
                {
                    if (root.TryGetProperty(field, out var thumbProp))
                    {
                        bannerUrl = thumbProp.GetString();
                        if (!string.IsNullOrEmpty(bannerUrl))
                        {
                            Debug.WriteLine($"[ChannelMonitor] Using {field}: {bannerUrl}");
                            break;
                        }
                    }
                }

                // Look in thumbnails array for avatar (identified by yt3.ggpht.com domain or avatar in URL)
                if (string.IsNullOrEmpty(bannerUrl) && root.TryGetProperty("thumbnails", out var thumbnails) &&
                    thumbnails.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var thumb in thumbnails.EnumerateArray())
                    {
                        if (thumb.TryGetProperty("url", out var urlProp))
                        {
                            string thumbUrl = urlProp.GetString() ?? "";
                            // Avatar thumbnails are typically on yt3.ggpht.com or have small square dimensions
                            if (thumbUrl.Contains("yt3.ggpht.com") || thumbUrl.Contains("/a/"))
                            {
                                bannerUrl = thumbUrl;
                                Debug.WriteLine($"[ChannelMonitor] Found avatar in thumbnails array: {bannerUrl}");
                                break;
                            }
                        }
                    }
                }

                // If no avatar found, use video thumbnail as fallback
                if (string.IsNullOrEmpty(bannerUrl) && root.TryGetProperty("thumbnails", out var vidThumbs) &&
                    vidThumbs.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    int maxSize = 0;
                    string? largestThumb = null;

                    foreach (var thumb in vidThumbs.EnumerateArray())
                    {
                        if (thumb.TryGetProperty("url", out var url))
                        {
                            string thumbUrl = url.GetString() ?? "";
                            // Skip avatar URLs we already looked for
                            if (thumbUrl.Contains("yt3.ggpht.com")) continue;

                            int width = 0;
                            if (thumb.TryGetProperty("width", out var w) && w.ValueKind == System.Text.Json.JsonValueKind.Number)
                            {
                                width = w.GetInt32();
                            }

                            if (thumbUrl.Contains("maxresdefault") || thumbUrl.Contains("hqdefault"))
                            {
                                if (width > maxSize || maxSize == 0)
                                {
                                    maxSize = width > 0 ? width : 1280;
                                    largestThumb = thumbUrl;
                                }
                            }
                            else if (width > maxSize)
                            {
                                maxSize = width;
                                largestThumb = thumbUrl;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(largestThumb))
                    {
                        bannerUrl = largestThumb;
                        Debug.WriteLine($"[ChannelMonitor] Using video thumbnail as channel visual: {bannerUrl}");
                    }
                }
            }

            // Method 3: Try direct YouTube channel avatar URL construction
            if (string.IsNullOrEmpty(bannerUrl) && !string.IsNullOrEmpty(channel.ChannelId))
            {
                bannerUrl = $"https://yt3.googleusercontent.com/ytc/{channel.ChannelId}=s800-c-k-c0x00ffffff-no-rj";
                Debug.WriteLine($"[ChannelMonitor] Trying constructed avatar URL: {bannerUrl}");
            }

            // Download the banner/thumbnail
            if (!string.IsNullOrEmpty(bannerUrl))
            {
                Debug.WriteLine($"[ChannelMonitor] Downloading banner from: {bannerUrl}");
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                try
                {
                    var imageData = await httpClient.GetByteArrayAsync(bannerUrl);

                    // Determine file extension from content
                    string extension = ".jpg";
                    if (bannerUrl.Contains(".png") || (imageData.Length > 8 &&
                        imageData[0] == 0x89 && imageData[1] == 0x50))
                    {
                        extension = ".png";
                    }
                    else if (bannerUrl.Contains(".webp") || (imageData.Length > 12 &&
                        System.Text.Encoding.ASCII.GetString(imageData, 8, 4) == "WEBP"))
                    {
                        extension = ".webp";
                    }

                    string bannerPath = Path.Combine(
                        storage.GetBannerStoragePath(),
                        $"{channel.ChannelId}{extension}");

                    await File.WriteAllBytesAsync(bannerPath, imageData);
                    channel.BannerPath = bannerPath;
                    channel.BannerLastUpdated = DateTime.Now;
                    Debug.WriteLine($"[ChannelMonitor] Banner saved to: {bannerPath}");
                }
                catch (HttpRequestException httpEx)
                {
                    Debug.WriteLine($"[ChannelMonitor] HTTP error downloading banner: {httpEx.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"[ChannelMonitor] No banner URL found for channel: {channel.ChannelName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Banner download error: {ex.Message}");
            // Banner download is optional, continue without it
        }
    }

    /// <summary>
    /// Fetch channel videos using --flat-playlist for speed.
    /// Always fetches ALL videos and merges with existing.
    /// Then update dates via RSS feed for the 15 most recent.
    /// </summary>
    private async Task FetchChannelVideosAsync(MonitoredChannel channel, bool fetchAll = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
            return;

        var existingVideoIds = channel.Videos.Select(v => v.VideoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool isRefresh = existingVideoIds.Count > 0;

        // Always fetch ALL videos - no limit. We merge with existing data.
        // Use --extractor-args to ensure we get all pages from YouTube
        Debug.WriteLine($"[ChannelMonitor] Fetching ALL videos for {channel.ChannelName} (refresh={isRefresh})");

        string cookiesArg = GetCookiesArgument();
        string arguments = $"{cookiesArg}--dump-json --flat-playlist --extractor-args \"youtube:approximate_date\" \"{channel.ChannelUrl}/videos\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? process = null;
        var newVideos = new List<ChannelVideo>();

        try
        {
            process = new Process { StartInfo = startInfo };
            process.Start();

            // Consume stderr asynchronously to prevent blocking
            _ = process.StandardError.ReadToEndAsync();

            int videoCount = 0;
            bool foundExisting = false;

            string modeInfo = isRefresh ? "Checking for new videos" : "Fetching videos";
            statusLabel.Text = $"{modeInfo} from {channel.ChannelName}...";
            statusLabel.Refresh();

            while (!process.StandardOutput.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try { process.Kill(); } catch { }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                string? line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    string videoId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(videoId)) continue;

                    videoCount++;

                    // If we hit an existing video during refresh, we've found all new ones
                    if (existingVideoIds.Contains(videoId))
                    {
                        if (isRefresh && !foundExisting)
                        {
                            foundExisting = true;
                            Debug.WriteLine($"[ChannelMonitor] Found existing video at position {videoCount}, stopping search");
                        }
                        continue;
                    }

                    // Check if already in download history
                    bool isInHistory = storage.IsVideoInDownloadHistory(videoId, downloadHistory);

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

                    if (videoCount % 10 == 0)
                    {
                        statusLabel.Text = $"Found {newVideos.Count} new videos from {channel.ChannelName}...";
                        statusLabel.Refresh();
                    }
                }
                catch
                {
                    // Skip malformed JSON lines
                }
            }

            await process.WaitForExitAsync();

            // Insert new videos at the BEGINNING (they are newest)
            if (newVideos.Count > 0)
            {
                channel.Videos.InsertRange(0, newVideos);
                Debug.WriteLine($"[ChannelMonitor] Added {newVideos.Count} new videos for {channel.ChannelName}");
            }

            channel.LastChecked = DateTime.Now;

            // Now fetch RSS feed to update dates for the 15 most recent videos
            statusLabel.Text = $"Updating video dates from RSS feed...";
            statusLabel.Refresh();
            await UpdateVideoDatesFromRssAsync(channel, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[ChannelMonitor] Fetch cancelled for {channel.ChannelName}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Error fetching videos: {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// Fetch YouTube RSS feed and update upload dates for matching videos.
    /// RSS feed provides dates for the 15 most recent videos.
    /// </summary>
    private async Task UpdateVideoDatesFromRssAsync(MonitoredChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(channel.ChannelId))
        {
            Debug.WriteLine($"[ChannelMonitor] No channel ID for RSS feed lookup");
            return;
        }

        string rssUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={channel.ChannelId}";
        Debug.WriteLine($"[ChannelMonitor] Fetching RSS feed: {rssUrl}");

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            string rssContent = await httpClient.GetStringAsync(rssUrl, cancellationToken);

            // Parse the RSS/Atom feed
            var xdoc = System.Xml.Linq.XDocument.Parse(rssContent);
            System.Xml.Linq.XNamespace atom = "http://www.w3.org/2005/Atom";
            System.Xml.Linq.XNamespace yt = "http://www.youtube.com/xml/schemas/2015";

            var entries = xdoc.Descendants(atom + "entry");
            int updatedCount = 0;

            foreach (var entry in entries)
            {
                // Extract video ID from yt:videoId element
                string? videoId = entry.Element(yt + "videoId")?.Value;
                if (string.IsNullOrEmpty(videoId)) continue;

                // Find matching video in our list
                var video = channel.Videos.FirstOrDefault(v =>
                    v.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));

                if (video != null && video.UploadDate == DateTime.MinValue)
                {
                    // Get published date
                    string? publishedStr = entry.Element(atom + "published")?.Value;
                    if (!string.IsNullOrEmpty(publishedStr) && DateTime.TryParse(publishedStr, out var publishedDate))
                    {
                        video.UploadDate = publishedDate.Date;
                        updatedCount++;
                        Debug.WriteLine($"[ChannelMonitor] Updated date for {videoId}: {video.UploadDate:yyyy-MM-dd}");
                    }
                }
            }

            Debug.WriteLine($"[ChannelMonitor] Updated {updatedCount} video dates from RSS feed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Error fetching RSS feed: {ex.Message}");
            // RSS fetch is optional - continue without dates
        }
    }

    /// <summary>
    /// Fetch channel videos with real-time progress updates in the grid.
    /// Used when adding a new channel to show immediate feedback.
    /// Uses --flat-playlist for speed (no limit), then updates dates via RSS.
    /// </summary>
    private async Task FetchChannelVideosWithProgressAsync(MonitoredChannel channel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
            return;

        // Fetch ALL videos - no limit. We merge with existing and RSS updates dates for recent 15.
        // Use --extractor-args to ensure we get all pages from YouTube
        Debug.WriteLine($"[ChannelMonitor] FetchWithProgress: fetching ALL videos for {channel.ChannelName}");
        string arguments = $"--dump-json --flat-playlist --extractor-args \"youtube:approximate_date\" \"{channel.ChannelUrl}/videos\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? process = null;
        try
        {
            process = new Process { StartInfo = startInfo };
            process.Start();

            // Consume stderr asynchronously to prevent blocking
            _ = process.StandardError.ReadToEndAsync();

            var existingVideoIds = channel.Videos.Select(v => v.VideoId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int videoCount = 0;

            statusLabel.Text = $"Fetching videos from {channel.ChannelName}...";
            statusLabel.Refresh();

            while (!process.StandardOutput.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    try { process.Kill(); } catch { }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                string? line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    string videoId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(videoId) || existingVideoIds.Contains(videoId))
                        continue;

                    videoCount++;

                    bool isInHistory = storage.IsVideoInDownloadHistory(videoId, downloadHistory);

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

                    // Add to channel (videos are in newest-first order from YouTube)
                    channel.Videos.Add(video);
                    existingVideoIds.Add(videoId);

                    // Update status periodically (but don't add to grid yet - wait for RSS)
                    if (videoCount % 5 == 0)
                    {
                        statusLabel.Text = $"Found {videoCount} videos from {channel.ChannelName}...";
                        statusLabel.Refresh();
                    }
                }
                catch
                {
                    // Skip malformed JSON lines
                }
            }

            await process.WaitForExitAsync();

            channel.LastChecked = DateTime.Now;

            // Fetch RSS feed to update dates for the 15 most recent videos BEFORE displaying
            if (videoCount > 0)
            {
                statusLabel.Text = $"Updating video dates from RSS feed...";
                statusLabel.Refresh();
                await UpdateVideoDatesFromRssAsync(channel, cancellationToken);
            }

            // NOW display the videos with correct dates
            if (selectedChannel?.ChannelId == channel.ChannelId)
            {
                DisplayVideos(channel);
            }

            storage.Save();

            if (videoCount == 0)
            {
                statusLabel.Text = $"No videos found for {channel.ChannelName}";
            }
            else
            {
                statusLabel.Text = $"Found {videoCount} videos from {channel.ChannelName}";
            }
            Debug.WriteLine($"[ChannelMonitor] Added {videoCount} videos for {channel.ChannelName}");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[ChannelMonitor] Fetch cancelled for {channel.ChannelName}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Error fetching videos: {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }
    /// <summary>
    /// Add a single video row to the grid (for real-time updates)
    /// </summary>
    private void AddVideoRowToGrid(ChannelVideo video)
    {
        // Check if we should apply the max videos limit
        int maxVideos = config.MaxChannelVideosShown;
        if (maxVideos > 0 && videosGrid.Rows.Count >= maxVideos)
        {
            return;  // Don't add more rows if at limit
        }

        // Check if video should be hidden based on checkbox states
        if (!showAllVideosCheckBox.Checked &&
            (video.Status == VideoStatus.Ignored || video.Status == VideoStatus.Downloaded))
        {
            return;  // Skip ignored/downloaded videos unless showing all
        }

        int rowIndex = videosGrid.Rows.Add();
        var row = videosGrid.Rows[rowIndex];

        row.Cells["RowNum"].Value = videosGrid.Rows.Count;  // Row number based on current count
        row.Cells["Title"].Value = video.Title;
        row.Cells["VideoId"].Value = video.VideoId;
        row.Cells["UploadDate"].Value = video.UploadDate == DateTime.MinValue
            ? "N/A"
            : video.UploadDate.ToString("yyyy-MM-dd");
        row.Cells["Duration"].Value = FormatDuration(video.Duration);
        row.Cells["Status"].Value = video.Status.ToString();

        row.Tag = video;

        // Highlight based on status and configure buttons
        if (video.Status == VideoStatus.New)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);  // Light yellow
            row.Cells["Snooze"].Value = "Snooze";
            row.Cells["Ignore"].Value = "Ignore";
        }
        else if (video.Status == VideoStatus.Downloaded)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 200);  // Light green
            row.Cells["Download"].Value = "Play";
            row.Cells["Snooze"].Value = "";
            row.Cells["Ignore"].Value = "";
        }
        else if (video.Status == VideoStatus.Watched)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);  // Light blue
            row.Cells["Snooze"].Value = "Unsnooze";
            row.Cells["Ignore"].Value = "Ignore";
        }
        else if (video.Status == VideoStatus.Snoozed)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);  // Light gray
            row.Cells["Snooze"].Value = "Unsnooze";
            row.Cells["Ignore"].Value = "Ignore";
        }
        else if (video.Status == VideoStatus.Ignored)
        {
            row.DefaultCellStyle.ForeColor = Color.Gray;
            row.Cells["Snooze"].Value = "Unsnooze";
            row.Cells["Ignore"].Value = "Wake";
        }

        // Scroll to show new row
        if (videosGrid.Rows.Count <= 5)
        {
            videosGrid.FirstDisplayedScrollingRowIndex = 0;
        }
    }

    /// <summary>
    /// Fetch detailed video info to get upload date and duration (used for single video lookups)
    /// </summary>
    private async Task<(DateTime uploadDate, TimeSpan? duration)?> GetVideoDetailedInfoAsync(string videoId)
    {
        if (string.IsNullOrEmpty(config.YtDlpPath) || !File.Exists(config.YtDlpPath))
            return null;

        string cookiesArg = GetCookiesArgument();
        var startInfo = new ProcessStartInfo
        {
            FileName = config.YtDlpPath,
            Arguments = $"{cookiesArg}--dump-json --skip-download \"https://www.youtube.com/watch?v={videoId}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;

                DateTime? uploadDate = null;
                TimeSpan? duration = null;

                // Try release_date first (more accurate for scheduled releases), then upload_date
                string[] dateFields = { "release_date", "upload_date" };
                foreach (var dateField in dateFields)
                {
                    if (uploadDate == null && root.TryGetProperty(dateField, out var dateValue))
                    {
                        string? dateStr = dateValue.GetString();
                        if (!string.IsNullOrEmpty(dateStr) && dateStr.Length == 8)
                        {
                            if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                                System.Globalization.DateTimeStyles.None, out var parsedDate))
                            {
                                uploadDate = parsedDate;
                                Debug.WriteLine($"[ChannelMonitor] Got {dateField} for {videoId}: {parsedDate:yyyy-MM-dd}");
                            }
                        }
                    }
                }

                // Try timestamp as fallback
                if (uploadDate == null && root.TryGetProperty("timestamp", out var timestamp) &&
                    timestamp.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    long unixTime = timestamp.GetInt64();
                    uploadDate = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                    Debug.WriteLine($"[ChannelMonitor] Got timestamp for {videoId}: {uploadDate:yyyy-MM-dd}");
                }

                // Get duration
                if (root.TryGetProperty("duration", out var durationValue) &&
                    durationValue.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    duration = TimeSpan.FromSeconds(durationValue.GetDouble());
                }

                if (uploadDate.HasValue)
                {
                    return (uploadDate.Value, duration);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Failed to get detailed info for {videoId}: {ex.Message}");
        }

        return null;
    }

    private void RemoveChannelButton_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to stop monitoring '{selectedChannel.ChannelName}'?",
            "Confirm Remove",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            storage.RemoveChannel(selectedChannel.ChannelId);
            LoadChannels();
            ClearChannelDisplay();
            UpdateStatusLabel();
        }
    }

    private void ResetChannelButton_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        int ignoredCount = selectedChannel.Videos.Count(v => v.Status == VideoStatus.Ignored);

        // Show custom dialog with 3 options
        using var resetDialog = new ResetChannelDialog(selectedChannel.ChannelName, ignoredCount);
        var result = resetDialog.ShowDialog(this);

        if (result == DialogResult.Cancel) return;

        bool includeIgnored = resetDialog.ResetIgnored;
        string channelId = selectedChannel.ChannelId;
        storage.ResetChannel(channelId, includeIgnored);

        // Refresh the selected channel reference
        selectedChannel = storage.GetChannel(channelId);

        // Reload channels list (this clears selection)
        LoadChannels();

        // Re-select the channel to preserve selection
        for (int i = 0; i < channelListBox.Items.Count; i++)
        {
            if (channelListBox.Items[i] is ChannelListItem item &&
                item.Channel.ChannelId == channelId)
            {
                channelListBox.SelectedIndex = i;
                break;
            }
        }

        UpdateStatusLabel();
        string ignoredMsg = includeIgnored ? " (including ignored videos)" : "";
        statusLabel.Text = $"Channel reset{ignoredMsg}. Click Refresh to scan for new videos.";
    }

    private async void RefreshBannerMenuItem_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        statusLabel.Text = $"Refreshing banner for {selectedChannel.ChannelName}...";

        try
        {
            // Delete existing banner if present
            if (!string.IsNullOrEmpty(selectedChannel.BannerPath) && File.Exists(selectedChannel.BannerPath))
            {
                try { File.Delete(selectedChannel.BannerPath); } catch { }
                selectedChannel.BannerPath = "";
            }

            await DownloadChannelBannerAsync(selectedChannel, forceRefresh: true);
            storage.UpdateChannel(selectedChannel);

            // Reload the banner display
            LoadChannelBanner(selectedChannel);

            statusLabel.Text = string.IsNullOrEmpty(selectedChannel.BannerPath)
                ? "Could not download banner"
                : $"Banner refreshed for {selectedChannel.ChannelName}";
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error refreshing banner: {ex.Message}";
        }
    }

    private void OpenChannelMenuItem_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null || string.IsNullOrEmpty(selectedChannel.ChannelUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = selectedChannel.ChannelUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open browser: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SnoozeAllButton_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        int snoozedCount = 0;
        foreach (var video in selectedChannel.Videos.Where(v => v.Status == VideoStatus.New))
        {
            video.Status = VideoStatus.Snoozed;
            video.IsNew = false;
            snoozedCount++;
        }

        if (snoozedCount > 0)
        {
            storage.Save();
            DisplayVideos(selectedChannel);
            UpdateStatsLabel(selectedChannel);
            statusLabel.Text = $"Snoozed {snoozedCount} video(s)";
        }
        else
        {
            statusLabel.Text = "No new videos to snooze";
        }
    }

    private void CancelFetchButton_Click(object? sender, EventArgs e)
    {
        fetchCancellation?.Cancel();
        statusLabel.Text = "Cancelling fetch...";
    }

    private void StatusFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        // Refresh the display based on filter selection
        DisplayVideos(selectedChannel);
    }

    private void ShowAllVideosCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        // Refresh the display to show/hide videos based on checkbox state
        DisplayVideos(selectedChannel);
    }

    private void SearchFilterTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        // Refresh the display based on search text
        DisplayVideos(selectedChannel);
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null) return;

        refreshButton.Enabled = false;
        cancelFetchButton.Visible = true;
        statusLabel.Text = $"Checking for new videos from {selectedChannel.ChannelName}...";

        // Create cancellation token for this operation
        fetchCancellation?.Dispose();
        fetchCancellation = new CancellationTokenSource();

        try
        {
            int previousCount = selectedChannel.Videos.Count;
            await FetchChannelVideosAsync(selectedChannel, false, fetchCancellation.Token);

            if (fetchCancellation.Token.IsCancellationRequested)
            {
                statusLabel.Text = "Fetch cancelled.";
                return;
            }

            int newVideosFound = selectedChannel.Videos.Count - previousCount;

            storage.UpdateChannel(selectedChannel);
            DisplayChannel(selectedChannel);
            LoadChannels();  // Update list to show new video count
            UpdateStatusLabel();
            ResetCountdown();  // Reset countdown after manual refresh

            if (newVideosFound > 0)
            {
                statusLabel.Text = $"Found {newVideosFound} new video(s), {selectedChannel.Videos.Count} total videos";
            }
            else
            {
                statusLabel.Text = $"No new videos found. {selectedChannel.Videos.Count} total videos.";
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Fetch cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing channel: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            refreshButton.Enabled = true;
            cancelFetchButton.Visible = false;
        }
    }

    private async void RefreshAllButton_Click(object? sender, EventArgs e)
    {
        if (storage.Channels.Count == 0)
        {
            MessageBox.Show("No channels to refresh.", "No Channels",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        refreshAllButton.Enabled = false;
        refreshButton.Enabled = false;
        cancelFetchButton.Visible = true;

        // Create cancellation token for this operation
        fetchCancellation?.Dispose();
        fetchCancellation = new CancellationTokenSource();

        try
        {
            // Create a snapshot to avoid collection modified exception
            var channelsSnapshot = storage.Channels.ToList();
            int totalChannels = channelsSnapshot.Count;

            int count = 0;
            foreach (var channel in channelsSnapshot)
            {
                if (fetchCancellation.Token.IsCancellationRequested)
                {
                    statusLabel.Text = $"Refresh cancelled after {count}/{totalChannels} channels.";
                    break;
                }

                count++;
                statusLabel.Text = $"Refreshing {count}/{totalChannels}: {channel.ChannelName}...";
                await FetchChannelVideosAsync(channel, false, fetchCancellation.Token);
                storage.UpdateChannel(channel);
            }

            LoadChannels();
            if (selectedChannel != null)
            {
                DisplayChannel(selectedChannel);
            }
            UpdateStatusLabel();
            ResetCountdown();  // Reset countdown after Refresh All

            if (!fetchCancellation.Token.IsCancellationRequested)
            {
                statusLabel.Text = $"Refreshed all {totalChannels} channel(s)";
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Refresh All cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error refreshing channels: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            refreshAllButton.Enabled = true;
            refreshButton.Enabled = selectedChannel != null;
            cancelFetchButton.Visible = false;
        }
    }

    /// <summary>
    /// Public method to refresh all channels (called from MainForm idle timer)
    /// </summary>
    public async Task RefreshAllChannelsAsync()
    {
        // Create a snapshot to avoid collection modified exception
        var channelsSnapshot = storage.Channels.ToList();

        foreach (var channel in channelsSnapshot)
        {
            await FetchChannelVideosAsync(channel);
            storage.UpdateChannel(channel);
        }
    }

    private void VideosGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || selectedChannel == null) return;

        var video = videosGrid.Rows[e.RowIndex].Tag as ChannelVideo;
        if (video == null) return;

        string columnName = videosGrid.Columns[e.ColumnIndex].Name;

        if (columnName == "Download")
        {
            // If already downloaded, "Play" button plays the song
            if (video.Status == VideoStatus.Downloaded)
            {
                PlayDownloadedSong(video);
            }
            else
            {
                DownloadVideo(video, e.RowIndex);
            }
        }
        else if (columnName == "Snooze")
        {
            // Snooze button toggles between New and Snoozed
            if (video.Status == VideoStatus.Downloaded) return;  // Can't change downloaded

            if (video.Status == VideoStatus.Snoozed)
            {
                UnsnoozeVideo(video, e.RowIndex);  // Back to New
            }
            else
            {
                SnoozeVideo(video, e.RowIndex);
            }
        }
        else if (columnName == "Ignore")
        {
            // If already ignored, "Wake" button un-ignores it
            if (video.Status == VideoStatus.Ignored)
            {
                WakeVideo(video, e.RowIndex);
            }
            else if (video.Status != VideoStatus.Downloaded)
            {
                // Don't allow ignoring downloaded videos
                IgnoreVideo(video, e.RowIndex);
            }
        }
        else if (columnName == "VideoId")
        {
            // Clicking on Video ID opens the video in browser and marks as Watched
            OpenVideoInBrowserAndMarkWatched(video, e.RowIndex);
        }
    }

    private void VideosGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || selectedChannel == null) return;

        var video = videosGrid.Rows[e.RowIndex].Tag as ChannelVideo;
        if (video == null) return;

        string columnName = videosGrid.Columns[e.ColumnIndex].Name;

        // Double-clicking the Title or VideoId cell opens the video in browser and marks as Watched
        if (columnName == "Title" || columnName == "VideoId")
        {
            // If Snoozed, change to Watched when opening
            OpenVideoInBrowserAndMarkWatched(video, e.RowIndex);
        }
    }

    private void DownloadVideo(ChannelVideo video, int rowIndex)
    {
        string videoUrl = $"https://www.youtube.com/watch?v={video.VideoId}";

        // Mark as Downloaded (we're handing it off to MainForm)
        video.Status = VideoStatus.Downloaded;
        video.IsNew = false;
        storage.Save();

        // Update the row to show "Play" button and Downloaded status
        // Keep it visible - don't hide until user manually refreshes
        if (rowIndex >= 0 && rowIndex < videosGrid.Rows.Count)
        {
            var row = videosGrid.Rows[rowIndex];
            row.Cells["Status"].Value = "Downloaded";
            row.Cells["Download"].Value = "Play";
            row.Cells["Snooze"].Value = "";  // Disable snooze
            row.Cells["Ignore"].Value = "";  // Disable ignore
            row.DefaultCellStyle.BackColor = Color.FromArgb(200, 255, 200);  // Light green
        }

        // Send URL to MainForm (just fills the URL, does not auto-start)
        mainForm.QueueDownloadFromMonitor(videoUrl, video.Title);

        // Switch focus to main app
        mainForm.Activate();
        mainForm.BringToFront();
        mainForm.Focus();

        // Update stats and channel list (new count), but DON'T refresh video grid
        // Keep the row visible until user manually refreshes or selects another channel
        LoadChannels();
        UpdateStatusLabel();
        if (selectedChannel != null)
        {
            UpdateStatsLabel(selectedChannel);
        }
    }

    private void SnoozeVideo(ChannelVideo video, int rowIndex)
    {
        video.Status = VideoStatus.Snoozed;
        video.IsNew = false;
        storage.Save();

        // Update row display to show snoozed style
        if (rowIndex >= 0 && rowIndex < videosGrid.Rows.Count)
        {
            var row = videosGrid.Rows[rowIndex];
            row.Cells["Status"].Value = "Snoozed";
            row.Cells["Snooze"].Value = "Unsnooze";  // Change to Unsnooze button
            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);  // Light gray
        }

        LoadChannels();  // Update new count in list
        UpdateStatusLabel();

        if (selectedChannel != null)
            UpdateStatsLabel(selectedChannel);
    }

    private void UnsnoozeVideo(ChannelVideo video, int rowIndex)
    {
        video.Status = VideoStatus.New;
        video.IsNew = true;
        storage.Save();

        // Update row display to show New style
        if (rowIndex >= 0 && rowIndex < videosGrid.Rows.Count)
        {
            var row = videosGrid.Rows[rowIndex];
            row.Cells["Status"].Value = "New";
            row.Cells["Snooze"].Value = "Snooze";  // Change back to Snooze button
            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);  // Light yellow
            row.DefaultCellStyle.ForeColor = Color.Black;
        }

        LoadChannels();  // Update new count in list
        UpdateStatusLabel();

        if (selectedChannel != null)
            UpdateStatsLabel(selectedChannel);
    }

    private void IgnoreVideo(ChannelVideo video, int rowIndex)
    {
        video.Status = VideoStatus.Ignored;
        video.IsNew = false;
        storage.Save();

        // Remove from grid unless filter shows ignored videos
        string selectedFilter = statusFilterComboBox.SelectedItem?.ToString() ?? "All";
        bool showingIgnored = selectedFilter == "Ignored" || (selectedFilter == "All" && showAllVideosCheckBox.Checked);

        if (!showingIgnored)
        {
            videosGrid.Rows.RemoveAt(rowIndex);
        }
        else
        {
            // Update row display to show ignored style
            var row = videosGrid.Rows[rowIndex];
            row.Cells["Status"].Value = "Ignored";
            row.Cells["Snooze"].Value = "Unsnooze";  // Can still unsnooze to New
            row.Cells["Ignore"].Value = "Wake";
            row.DefaultCellStyle.BackColor = Color.Empty;
            row.DefaultCellStyle.ForeColor = Color.Gray;
        }

        LoadChannels();  // Update new count in list
        UpdateStatusLabel();

        if (selectedChannel != null)
            UpdateStatsLabel(selectedChannel);
    }

    private void WakeVideo(ChannelVideo video, int rowIndex)
    {
        // Un-ignore: set back to New status
        video.Status = VideoStatus.New;
        video.IsNew = true;
        storage.Save();

        // Update row display - restore all buttons for New status
        var row = videosGrid.Rows[rowIndex];
        row.Cells["Status"].Value = "New";
        row.Cells["Snooze"].Value = "Snooze";  // Restore snooze button
        row.Cells["Ignore"].Value = "Ignore";  // Restore ignore button
        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);  // Light yellow
        row.DefaultCellStyle.ForeColor = Color.Black;

        LoadChannels();  // Update new count in list
        UpdateStatusLabel();

        if (selectedChannel != null)
            UpdateStatsLabel(selectedChannel);

        statusLabel.Text = $"Video '{video.Title}' moved back to New status";
    }

    private void PlayDownloadedSong(ChannelVideo video)
    {
        // Try to play the song in Song Browser using the video ID
        if (mainForm.OpenSongBrowserAndPlayByVideoId(video.VideoId))
        {
            statusLabel.Text = $"Playing: {video.Title}";
        }
        else
        {
            statusLabel.Text = $"Song not found in library for video ID: {video.VideoId}";
        }
    }

    private void ChannelNameLabel_Click(object? sender, EventArgs e)
    {
        if (selectedChannel == null || string.IsNullOrEmpty(selectedChannel.ChannelUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = selectedChannel.ChannelUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChannelMonitor] Error opening channel URL: {ex.Message}");
        }
    }

    private void OpenVideoInBrowserAndMarkWatched(ChannelVideo video, int rowIndex)
    {
        string videoUrl = $"https://www.youtube.com/watch?v={video.VideoId}";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = videoUrl,
                UseShellExecute = true
            });

            // Mark as Watched (only if it was New)
            if (video.Status == VideoStatus.New)
            {
                video.Status = VideoStatus.Watched;
                video.IsNew = false;
                storage.Save();

                // Update row display
                videosGrid.Rows[rowIndex].Cells["Status"].Value = "Watched";
                videosGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(220, 235, 255);  // Light blue

                LoadChannels();  // Update new count in list
                UpdateStatusLabel();

                // Update stats label
                if (selectedChannel != null)
                {
                    int newCount = selectedChannel.Videos.Count(v => v.Status == VideoStatus.New);
                    int totalCount = selectedChannel.Videos.Count;
                    int ignoredCount = selectedChannel.Videos.Count(v => v.Status == VideoStatus.Ignored);
                    videoStatsLabel.Text = $"New: {newCount} | Total: {totalCount} | Ignored: {ignoredCount}";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open browser: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Mark a video as downloaded (called when download completes)
    /// </summary>
    public void MarkVideoAsDownloaded(string videoId)
    {
        // Create a snapshot to avoid collection modified exception
        var channelsSnapshot = storage.Channels.ToList();

        foreach (var channel in channelsSnapshot)
        {
            var video = channel.Videos.FirstOrDefault(v =>
                v.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));

            if (video != null)
            {
                video.Status = VideoStatus.Downloaded;
                video.IsNew = false;
                storage.Save();

                if (selectedChannel?.ChannelId == channel.ChannelId)
                {
                    DisplayVideos(selectedChannel);
                }
                LoadChannels();
                UpdateStatusLabel();
                break;
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Cancel any in-progress fetch operations
        fetchCancellation?.Cancel();
        fetchCancellation?.Dispose();

        countdownTimer?.Stop();
        countdownTimer?.Dispose();
        channelBanner.Image?.Dispose();

        // Clean up temporary cookie file
        YouTubeCookieManager.DeleteCookieFile();

        base.OnFormClosing(e);
    }
}

/// <summary>
/// Helper class for channel list display
/// </summary>
internal class ChannelListItem
{
    public MonitoredChannel Channel { get; }
    private readonly string displayText;

    public ChannelListItem(MonitoredChannel channel, string displayText)
    {
        Channel = channel;
        this.displayText = displayText;
    }

    public override string ToString() => displayText;
}

/// <summary>
/// Form for adding a new channel
/// </summary>
public class AddChannelForm : Form
{
    private TextBox urlTextBox = null!;
    public string ChannelUrl { get; private set; } = "";

    public AddChannelForm()
    {
        Text = "Add Channel";
        Size = new Size(500, 180);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = "Enter YouTube channel URL:",
            Location = new Point(12, 15),
            Size = new Size(460, 20)
        };

        urlTextBox = new TextBox
        {
            Location = new Point(12, 40),
            Size = new Size(460, 23)
        };

        var hintLabel = new Label
        {
            Text = "Examples:\nhttps://www.youtube.com/@ChannelName\nhttps://www.youtube.com/channel/UCxxxxxx\nhttps://www.youtube.com/c/channelname",
            Location = new Point(12, 68),
            Size = new Size(460, 50),
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 8)
        };

        var okButton = new Button
        {
            Text = "Add",
            Location = new Point(310, 120),
            Size = new Size(75, 28),
            DialogResult = DialogResult.OK
        };
        okButton.Click += (s, e) => { ChannelUrl = urlTextBox.Text.Trim(); };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(395, 120),
            Size = new Size(75, 28),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(label);
        Controls.Add(urlTextBox);
        Controls.Add(hintLabel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}

/// <summary>
/// Dialog for reset channel with 3 options: Reset, Reset + Clear Ignores, Cancel
/// </summary>
public class ResetChannelDialog : Form
{
    public bool ResetIgnored { get; private set; } = false;

    public ResetChannelDialog(string channelName, int ignoredCount)
    {
        Text = "Confirm Reset";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        string ignoredNote = ignoredCount > 0 ? $"\n- Keep {ignoredCount} ignored video(s)" : "";

        var messageLabel = new Label
        {
            Text = $"Are you sure you want to reset '{channelName}'?\n\n" +
                   "This will:\n" +
                   "- Remove all non-Ignored videos from this channel\n" +
                   $"- Reset the 'Last Checked' date{ignoredNote}",
            Location = new Point(15, 15),
            AutoSize = true
        };

        Controls.Add(messageLabel);

        // Calculate form size based on label
        int formWidth = Math.Max(450, messageLabel.PreferredWidth + 40);
        int buttonY = messageLabel.PreferredHeight + 35;

        var resetButton = new Button
        {
            Text = "Reset",
            Location = new Point(formWidth - 310, buttonY),
            Size = new Size(90, 28)
        };
        resetButton.Click += (s, e) =>
        {
            ResetIgnored = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        var resetIgnoredButton = new Button
        {
            Text = "Reset + Ignores",
            Location = new Point(formWidth - 210, buttonY),
            Size = new Size(110, 28),
            Enabled = ignoredCount > 0
        };
        resetIgnoredButton.Click += (s, e) =>
        {
            ResetIgnored = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(formWidth - 90, buttonY),
            Size = new Size(80, 28),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(resetButton);
        Controls.Add(resetIgnoredButton);
        Controls.Add(cancelButton);

        // Set form size based on content
        ClientSize = new Size(formWidth, buttonY + 45);

        CancelButton = cancelButton;
    }
}
