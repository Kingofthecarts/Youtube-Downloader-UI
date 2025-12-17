namespace Youtube_Downloader;

public class OptionsForm : Form
{
    private MaskedTextBox delayTextBox = null!;
    private CheckBox embedThumbnailCheckBox = null!;
    private CheckBox sortFoldersByRecentCheckBox = null!;
    private TextBox maxFoldersTextBox = null!;
    private CheckBox monitorOneDriveCheckBox = null!;
    private NumericUpDown channelScanIntervalNumeric = null!;
    private CheckBox channelAutoScanCheckBox = null!;
    private NumericUpDown maxChannelVideosNumeric = null!;
    private CheckBox enablePerformanceTrackingCheckBox = null!;
    private Label lastSongBrowserOpenLabel = null!;
    private Label lastHistoryOpenLabel = null!;
    private Label lastSongLoadLabel = null!;
    private TextBox gitHubReleaseUrlTextBox = null!;
    private Button checkForUpdateButton = null!;
    private CheckBox autoCheckAppUpdatesCheckBox = null!;
    private CheckBox autoCheckYtDlpUpdatesCheckBox = null!;
    private CheckBox allowSongDeleteCheckBox = null!;
    private CheckBox songDeleteCountdownCheckBox = null!;
    private NumericUpDown songDeleteCountdownNumeric = null!;
    private CheckBox minimizeOnCloseCheckBox = null!;
    private CheckBox allowFilenameEditCheckBox = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;

    private readonly ToolsManager toolsManager;
    private readonly Config config;
    private readonly Logger logger;

    public OptionsForm(Config config, ToolsManager toolsManager, Logger logger)
    {
        this.config = config;
        this.toolsManager = toolsManager;
        this.logger = logger;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Options";
        Size = new Size(600, 1030);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 20;

        // Download Delay section
        var delayLabel = new Label
        {
            Text = "Delay between downloads (MM:SS):",
            Location = new Point(15, y),
            AutoSize = true
        };

        delayTextBox = new MaskedTextBox
        {
            Location = new Point(220, y - 2),
            Size = new Size(60, 23),
            Mask = "00:00",
            Text = FormatDelayTime(config.DownloadDelaySeconds),
            TextAlign = HorizontalAlignment.Center
        };

        var delayHintLabel = new Label
        {
            Text = "(0 = no delay, max 59:59)",
            Location = new Point(285, y),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 35;

        Controls.Add(delayLabel);
        Controls.Add(delayTextBox);
        Controls.Add(delayHintLabel);

        // Embed Thumbnail section
        embedThumbnailCheckBox = new CheckBox
        {
            Text = "Embed YouTube thumbnail as album art",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.EmbedThumbnail
        };
        y += 35;

        Controls.Add(embedThumbnailCheckBox);

        // Folder History section
        var folderHistorySectionLabel = new Label
        {
            Text = "Folder History:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        sortFoldersByRecentCheckBox = new CheckBox
        {
            Text = "Sort folder dropdown by most recently used (default: alphabetically)",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.SortFoldersByRecent
        };
        y += 25;

        var maxFoldersLabel = new Label
        {
            Text = "Folders saved:",
            Location = new Point(15, y),
            AutoSize = true
        };

        // Get current folder count
        var tempFolderHistory = new FolderHistory();
        int currentFolderCount = tempFolderHistory.Folders.Count;

        maxFoldersTextBox = new TextBox
        {
            Location = new Point(100, y - 2),
            Size = new Size(80, 23),
            Text = $"{currentFolderCount} / {FolderHistory.MaxFolders}",
            ReadOnly = true,
            BackColor = SystemColors.Control,
            TextAlign = HorizontalAlignment.Center
        };
        y += 40;

        Controls.Add(folderHistorySectionLabel);
        Controls.Add(sortFoldersByRecentCheckBox);
        Controls.Add(maxFoldersLabel);
        Controls.Add(maxFoldersTextBox);

        // OneDrive Monitoring section
        var oneDriveSectionLabel = new Label
        {
            Text = "OneDrive Monitoring:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        monitorOneDriveCheckBox = new CheckBox
        {
            Text = "Monitor OneDrive status (warn if not running on startup, check every 5 min)",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.MonitorOneDrive
        };
        y += 40;

        Controls.Add(oneDriveSectionLabel);
        Controls.Add(monitorOneDriveCheckBox);

        // Channel Monitor section
        var channelMonitorSectionLabel = new Label
        {
            Text = "Channel Monitor:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        var channelScanIntervalLabel = new Label
        {
            Text = "Scan interval (minutes):",
            Location = new Point(15, y),
            AutoSize = true
        };

        channelScanIntervalNumeric = new NumericUpDown
        {
            Location = new Point(160, y - 2),
            Size = new Size(60, 23),
            Minimum = 1,
            Maximum = 60,
            Value = config.ChannelScanIntervalMinutes
        };
        y += 25;

        channelAutoScanCheckBox = new CheckBox
        {
            Text = "Enable auto-scan",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.ChannelAutoScanEnabled
        };
        y += 25;

        var maxChannelVideosLabel = new Label
        {
            Text = "Max videos shown per channel:",
            Location = new Point(15, y),
            AutoSize = true
        };

        maxChannelVideosNumeric = new NumericUpDown
        {
            Location = new Point(200, y - 2),
            Size = new Size(60, 23),
            Minimum = 0,
            Maximum = 500,
            Value = config.MaxChannelVideosShown
        };

        var maxVideosHintLabel = new Label
        {
            Text = "(0 = unlimited)",
            Location = new Point(265, y),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 40;

        Controls.Add(channelMonitorSectionLabel);
        Controls.Add(channelScanIntervalLabel);
        Controls.Add(channelScanIntervalNumeric);
        Controls.Add(channelAutoScanCheckBox);
        Controls.Add(maxChannelVideosLabel);
        Controls.Add(maxChannelVideosNumeric);
        Controls.Add(maxVideosHintLabel);

        // Performance Monitoring section
        var performanceSectionLabel = new Label
        {
            Text = "Performance Monitoring:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        enablePerformanceTrackingCheckBox = new CheckBox
        {
            Text = "Enable performance tracking",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.EnablePerformanceTracking
        };
        y += 25;

        lastSongBrowserOpenLabel = new Label
        {
            Text = $"Last Song Browser open time: {config.LastSongBrowserOpenMs} ms",
            Location = new Point(15, y),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 20;

        lastHistoryOpenLabel = new Label
        {
            Text = $"Last History open time: {config.LastHistoryOpenMs} ms",
            Location = new Point(15, y),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 20;

        lastSongLoadLabel = new Label
        {
            Text = $"Last Song Load time: {config.LastSongLoadMs} ms",
            Location = new Point(15, y),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 40;

        Controls.Add(performanceSectionLabel);
        Controls.Add(enablePerformanceTrackingCheckBox);
        Controls.Add(lastSongBrowserOpenLabel);
        Controls.Add(lastHistoryOpenLabel);
        Controls.Add(lastSongLoadLabel);

        // Updates section
        var updatesSectionLabel = new Label
        {
            Text = "Updates:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        var gitHubReleaseUrlLabel = new Label
        {
            Text = "GitHub Repository URL (e.g., https://github.com/user/repo):",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 20;

        gitHubReleaseUrlTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = config.GitHubReleaseUrl,
            PlaceholderText = "https://github.com/username/repository"
        };

        checkForUpdateButton = new Button
        {
            Text = "Check for Update",
            Location = new Point(420, y - 1),
            Size = new Size(150, 25)
        };
        checkForUpdateButton.Click += CheckForUpdateButton_Click;
        y += 30;

        autoCheckAppUpdatesCheckBox = new CheckBox
        {
            Text = "Check for app updates on startup",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.AutoCheckAppUpdates
        };
        y += 25;

        autoCheckYtDlpUpdatesCheckBox = new CheckBox
        {
            Text = "Check for yt-dlp updates on startup",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.AutoCheckYtDlpUpdates
        };
        y += 35;

        Controls.Add(updatesSectionLabel);
        Controls.Add(gitHubReleaseUrlLabel);
        Controls.Add(gitHubReleaseUrlTextBox);
        Controls.Add(checkForUpdateButton);
        Controls.Add(autoCheckAppUpdatesCheckBox);
        Controls.Add(autoCheckYtDlpUpdatesCheckBox);

        // Song Browser Delete Feature section
        var songDeleteSectionLabel = new Label
        {
            Text = "Song Browser Delete Feature:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        allowSongDeleteCheckBox = new CheckBox
        {
            Text = "Allow deleting songs from Song Browser",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.AllowSongDelete
        };
        allowSongDeleteCheckBox.CheckedChanged += (s, e) => UpdateDeleteCountdownEnabled();
        y += 25;

        songDeleteCountdownCheckBox = new CheckBox
        {
            Text = "Require countdown before delete",
            Location = new Point(35, y),
            AutoSize = true,
            Checked = config.SongDeleteRequireCountdown,
            Enabled = config.AllowSongDelete
        };
        y += 25;

        var countdownLabel = new Label
        {
            Text = "Countdown seconds:",
            Location = new Point(35, y),
            AutoSize = true
        };

        songDeleteCountdownNumeric = new NumericUpDown
        {
            Location = new Point(160, y - 2),
            Size = new Size(60, 23),
            Minimum = 1,
            Maximum = 30,
            Value = config.SongDeleteCountdownSeconds,
            Enabled = config.AllowSongDelete
        };
        y += 45;

        Controls.Add(songDeleteSectionLabel);
        Controls.Add(allowSongDeleteCheckBox);
        Controls.Add(songDeleteCountdownCheckBox);
        Controls.Add(countdownLabel);
        Controls.Add(songDeleteCountdownNumeric);

        // Window Behavior section
        var windowBehaviorSectionLabel = new Label
        {
            Text = "Window Behavior:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        minimizeOnCloseCheckBox = new CheckBox
        {
            Text = "Minimize to taskbar when closing (instead of exiting)",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.MinimizeOnClose
        };
        y += 40;

        Controls.Add(windowBehaviorSectionLabel);
        Controls.Add(minimizeOnCloseCheckBox);

        // Song Editor section
        var songEditorSectionLabel = new Label
        {
            Text = "Song Editor:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        allowFilenameEditCheckBox = new CheckBox
        {
            Text = "Allow editing filename (disabled by default for safety)",
            Location = new Point(15, y),
            AutoSize = true,
            Checked = config.AllowFilenameEdit
        };
        y += 40;

        Controls.Add(songEditorSectionLabel);
        Controls.Add(allowFilenameEditCheckBox);

        // Logs section
        var logsSectionLabel = new Label
        {
            Text = "Logs:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        var logLocationLabel = new Label
        {
            Text = "Log folder:",
            Location = new Point(15, y),
            AutoSize = true
        };

        var logLocationTextBox = new TextBox
        {
            Location = new Point(90, y - 2),
            Size = new Size(350, 23),
            Text = logger.LogDirectory,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };

        var openLogFolderButton = new Button
        {
            Text = "Open",
            Location = new Point(445, y - 3),
            Size = new Size(60, 25)
        };
        openLogFolderButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logger.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch { }
        };
        y += 28;

        var clearLogsButton = new Button
        {
            Text = "Clear Old Logs",
            Location = new Point(15, y),
            Size = new Size(110, 25)
        };
        clearLogsButton.Click += ClearLogsButton_Click;

        var clearLogsHintLabel = new Label
        {
            Text = "(keeps last 20 log files)",
            Location = new Point(130, y + 4),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        y += 40;

        Controls.Add(logsSectionLabel);
        Controls.Add(logLocationLabel);
        Controls.Add(logLocationTextBox);
        Controls.Add(openLogFolderButton);
        Controls.Add(clearLogsButton);
        Controls.Add(clearLogsHintLabel);

        // Buttons
        saveButton = new Button
        {
            Text = "Save",
            Location = new Point(400, y),
            Size = new Size(80, 28)
        };
        saveButton.Click += SaveButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(490, y),
            Size = new Size(80, 28)
        };
        cancelButton.Click += (s, e) => Close();

        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void UpdateDeleteCountdownEnabled()
    {
        bool enabled = allowSongDeleteCheckBox.Checked;
        songDeleteCountdownCheckBox.Enabled = enabled;
        songDeleteCountdownNumeric.Enabled = enabled;
    }

    private string FormatDelayTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2}{seconds:D2}";
    }

    private int ParseDelayTime(string text)
    {
        // Text format is "MM:SS" but mask removes the colon for value
        string cleanText = text.Replace(":", "").Replace(" ", "0");
        if (cleanText.Length >= 4)
        {
            if (int.TryParse(cleanText.Substring(0, 2), out int minutes) &&
                int.TryParse(cleanText.Substring(2, 2), out int seconds))
            {
                // Clamp seconds to 59
                seconds = Math.Min(seconds, 59);
                minutes = Math.Min(minutes, 59);
                return minutes * 60 + seconds;
            }
        }
        return 0;
    }

    private async void CheckForUpdateButton_Click(object? sender, EventArgs e)
    {
        string repoUrl = gitHubReleaseUrlTextBox.Text.Trim();

        if (string.IsNullOrEmpty(repoUrl))
        {
            MessageBox.Show(
                "Please enter a GitHub Repository URL first.\n\n" +
                "Example: https://github.com/username/repository",
                "URL Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Validate URL
        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
        {
            MessageBox.Show(
                "The URL format is invalid.\n\nPlease enter a valid GitHub repository URL.",
                "Invalid URL",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Save the URL to config
        config.GitHubReleaseUrl = repoUrl;
        config.Save();

        // Check for updates first
        checkForUpdateButton.Enabled = false;
        checkForUpdateButton.Text = "Checking...";

        try
        {
            var updateResult = await AppUpdater.CheckForUpdateAsync(repoUrl);

            if (!string.IsNullOrEmpty(updateResult.ErrorMessage))
            {
                MessageBox.Show(
                    $"Failed to check for updates:\n\n{updateResult.ErrorMessage}",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!updateResult.UpdateAvailable)
            {
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
                return;
            }

            // Show progress dialog
            using var progressForm = new DownloadProgressForm("Downloading Update");

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

            if (progressForm.WasSuccessful)
            {
                var result = MessageBox.Show(
                    $"Update v{updateResult.LatestVersion} downloaded successfully!\n\n" +
                    "The application needs to restart to apply the update.\n\n" +
                    "Do you want to restart now?",
                    "Update Ready",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        AppUpdater.LaunchUpdateScript();
                        Application.Exit();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to launch update script:\n{ex.Message}",
                            "Update Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            else if (!progressForm.WasCancelled)
            {
                MessageBox.Show(
                    "Failed to download the update.\n\n" +
                    "Please try again later.",
                    "Download Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            checkForUpdateButton.Enabled = true;
            checkForUpdateButton.Text = "Check for Update";
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // Get values from controls
        int delaySeconds = ParseDelayTime(delayTextBox.Text);
        bool embedThumbnail = embedThumbnailCheckBox.Checked;
        bool sortFoldersByRecent = sortFoldersByRecentCheckBox.Checked;
        bool monitorOneDrive = monitorOneDriveCheckBox.Checked;
        int channelScanIntervalMinutes = (int)channelScanIntervalNumeric.Value;
        bool channelAutoScanEnabled = channelAutoScanCheckBox.Checked;
        int maxChannelVideosShown = (int)maxChannelVideosNumeric.Value;
        bool enablePerformanceTracking = enablePerformanceTrackingCheckBox.Checked;
        string gitHubReleaseUrl = gitHubReleaseUrlTextBox.Text.Trim();
        bool autoCheckAppUpdates = autoCheckAppUpdatesCheckBox.Checked;
        bool autoCheckYtDlpUpdates = autoCheckYtDlpUpdatesCheckBox.Checked;
        bool allowSongDelete = allowSongDeleteCheckBox.Checked;
        bool songDeleteCountdown = songDeleteCountdownCheckBox.Checked;
        int songDeleteCountdownSeconds = (int)songDeleteCountdownNumeric.Value;
        bool minimizeOnClose = minimizeOnCloseCheckBox.Checked;
        bool allowFilenameEdit = allowFilenameEditCheckBox.Checked;

        // Save to config
        config.DownloadDelaySeconds = delaySeconds;
        config.EmbedThumbnail = embedThumbnail;
        config.SortFoldersByRecent = sortFoldersByRecent;
        config.MonitorOneDrive = monitorOneDrive;
        config.ChannelScanIntervalMinutes = channelScanIntervalMinutes;
        config.ChannelAutoScanEnabled = channelAutoScanEnabled;
        config.MaxChannelVideosShown = maxChannelVideosShown;
        config.EnablePerformanceTracking = enablePerformanceTracking;
        config.GitHubReleaseUrl = gitHubReleaseUrl;
        config.AutoCheckAppUpdates = autoCheckAppUpdates;
        config.AutoCheckYtDlpUpdates = autoCheckYtDlpUpdates;
        config.AllowSongDelete = allowSongDelete;
        config.SongDeleteRequireCountdown = songDeleteCountdown;
        config.SongDeleteCountdownSeconds = songDeleteCountdownSeconds;
        config.MinimizeOnClose = minimizeOnClose;
        config.AllowFilenameEdit = allowFilenameEdit;

        // Save immediately
        config.Save();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ClearLogsButton_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "This will delete old log files, keeping only the 20 most recent.\n\nContinue?",
            "Clear Old Logs",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            logger.ClearOldLogs(20);
            MessageBox.Show(
                "Old log files have been cleared.",
                "Logs Cleared",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
