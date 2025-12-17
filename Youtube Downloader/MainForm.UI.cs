namespace Youtube_Downloader;

/// <summary>
/// Partial class containing UI initialization methods for MainForm.
/// </summary>
public partial class MainForm
{
    private void InitializeComponents()
    {
        // Form settings
        Text = $"YouTube Downloader v{AppUpdater.CurrentVersionString}";
        Size = new Size(580, 510);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(500, 510);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        // Ensure form can be focused when clicked during processing
        Activated += (s, e) =>
        {
            if (!cancelButton.Focused && cancelButton.Visible)
            {
                cancelButton.Focus();
            }
        };

        InitializeMenuStrip();

        int y = menuStrip.Height + 5;

        InitializeTabs(ref y);
        InitializeProgressSection(ref y);
        InitializeStatusSection(ref y);
        InitializeButtons(ref y);
        InitializeMediaPlayerSection();  // No ref y - positioned independently at bottom
        InitializeStatusStrip();
        InitializeSharedMediaPlayerEvents();

        // Add controls to form
        Controls.Add(menuStrip);
        Controls.Add(downloadTabControl);
        Controls.Add(downloadLabel);
        Controls.Add(downloadProgressBar);
        Controls.Add(convertLabel);
        Controls.Add(convertProgressBar);
        Controls.Add(playlistLabel);
        Controls.Add(playlistProgressBar);
        Controls.Add(playlistProgressLabel);
        playlistProgressLabel.BringToFront();
        Controls.Add(statusLabel);
        Controls.Add(sourceLink);
        Controls.Add(destinationLink);
        Controls.Add(playSongButton);
        Controls.Add(clearAllButton);
        Controls.Add(cancelButton);
        if (mediaPlayerPanel != null) Controls.Add(mediaPlayerPanel);
        Controls.Add(statusStrip);
    }

    private void InitializeMenuStrip()
    {
        menuStrip = new MenuStrip();

        // File menu
        var fileMenu = new ToolStripMenuItem("File");
        var openOutputFolderMenuItem = new ToolStripMenuItem("Open Output Folder", null, OpenOutputFolderMenuItem_Click)
        {
            ShortcutKeys = Keys.Control | Keys.O
        };
        var exitMenuItem = new ToolStripMenuItem("Exit", null, ExitMenuItem_Click)
        {
            ShortcutKeys = Keys.Alt | Keys.F4
        };
        fileMenu.DropDownItems.Add(openOutputFolderMenuItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(exitMenuItem);

        // Songs menu (top-level)
        var songsMenu = new ToolStripMenuItem("Songs", null, BrowseSongsMenuItem_Click)
        {
            ShortcutKeys = Keys.Control | Keys.B
        };

        // History menu
        var historyMenu = new ToolStripMenuItem("History");
        var viewHistoryMenuItem = new ToolStripMenuItem("View History...", null, HistoryMenuItem_Click)
        {
            ShortcutKeys = Keys.Control | Keys.H
        };
        var clearHistoryMenuItem = new ToolStripMenuItem("Clear All History...", null, ClearHistoryMenuItem_Click);
        historyMenu.DropDownItems.Add(viewHistoryMenuItem);
        historyMenu.DropDownItems.Add(new ToolStripSeparator());
        historyMenu.DropDownItems.Add(clearHistoryMenuItem);

        // Options menu (new top-level menu)
        var optionsMenu = new ToolStripMenuItem("Options", null, OptionsMenuItem_Click);

        // Tools menu
        var toolsMenu = new ToolStripMenuItem("Tools");
        var editConfigMenuItem = new ToolStripMenuItem("Edit Configuration...", null, EditConfigMenuItem_Click);
        var folderListMenuItem = new ToolStripMenuItem("Folder List...", null, FolderListMenuItem_Click);
        var checkForUpdatesMenuItem = new ToolStripMenuItem("Check for Updates...", null, CheckForUpdatesMenuItem_Click);
        var changelogMenuItem = new ToolStripMenuItem("Changelog...", null, ChangelogMenuItem_Click);
        var redownloadFfmpegMenuItem = new ToolStripMenuItem("Redownload ffmpeg...", null, RedownloadFfmpegMenuItem_Click);
        var redownloadYtDlpMenuItem = new ToolStripMenuItem("Redownload yt-dlp...", null, RedownloadYtDlpMenuItem_Click);
        var redownloadDenoMenuItem = new ToolStripMenuItem("Redownload Deno...", null, RedownloadDenoMenuItem_Click);
        var reinstallWebView2MenuItem = new ToolStripMenuItem("Install/Update WebView2...", null, ReinstallWebView2MenuItem_Click);
        youtubeSignInMenuItem = new ToolStripMenuItem("Sign in to YouTube...", null, YouTubeSignInMenuItem_Click);
        youtubeSignOutMenuItem = new ToolStripMenuItem("Sign out of YouTube", null, YouTubeSignOutMenuItem_Click);
        toolsMenu.DropDownItems.Add(editConfigMenuItem);
        toolsMenu.DropDownItems.Add(folderListMenuItem);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(checkForUpdatesMenuItem);
        toolsMenu.DropDownItems.Add(changelogMenuItem);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(redownloadFfmpegMenuItem);
        toolsMenu.DropDownItems.Add(redownloadYtDlpMenuItem);
        toolsMenu.DropDownItems.Add(redownloadDenoMenuItem);
        toolsMenu.DropDownItems.Add(reinstallWebView2MenuItem);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(youtubeSignInMenuItem);
        toolsMenu.DropDownItems.Add(youtubeSignOutMenuItem);

        // Channels menu (for channel monitoring)
        var channelsMenu = new ToolStripMenuItem("Channels", null, ChannelsMenuItem_Click)
        {
            ShortcutKeys = Keys.Control | Keys.M
        };

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(songsMenu);
        menuStrip.Items.Add(channelsMenu);
        menuStrip.Items.Add(historyMenu);
        menuStrip.Items.Add(optionsMenu);
        menuStrip.Items.Add(toolsMenu);
        MainMenuStrip = menuStrip;
    }

    private void InitializeTabs(ref int y)
    {
        // === Tab Control for Single Video / Playlist ===
        downloadTabControl = new TabControl
        {
            Location = new Point(12, y),
            Size = new Size(540, 135)
        };

        InitializeSingleVideoTab();
        InitializePlaylistTab();

        y += 145;
    }

    private void InitializeSingleVideoTab()
    {
        var singleTab = new TabPage("Single Video");

        var urlLabel = new Label
        {
            Text = "YouTube URL:",
            Location = new Point(10, 12),
            AutoSize = true
        };

        var singlePasteButton = new Button
        {
            Text = "Paste",
            Location = new Point(95, 8),
            Size = new Size(50, 23)
        };
        singlePasteButton.Click += (s, e) =>
        {
            if (Clipboard.ContainsText())
            {
                urlTextBox.Text = Clipboard.GetText().Trim();
            }
        };

        urlTextBox = new TextBox
        {
            Location = new Point(10, 32),
            Size = new Size(430, 23)
        };
        urlTextBox.TextChanged += (s, e) => UpdateGoButtonEnabled();
        urlTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                GoButton_Click(s, e);
            }
        };

        goButton = new Button
        {
            Text = "Go",
            Location = new Point(445, 31),
            Size = new Size(75, 25),
            Enabled = false
        };
        goButton.Click += GoButton_Click;

        var singleFolderLabel = new Label
        {
            Text = "Folder (optional):",
            Location = new Point(10, 65),
            AutoSize = true
        };

        singleFolderComboBox = new ComboBox
        {
            Location = new Point(110, 62),
            Size = new Size(295, 23),
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        singleClearFolderButton = new Button
        {
            Text = "Clear",
            Location = new Point(410, 61),
            Size = new Size(50, 25)
        };
        singleClearFolderButton.Click += (s, e) => singleFolderComboBox.Text = "";

        allowRenameCheckBox = new CheckBox
        {
            Text = "Rename after download (edit filename, album, song name)",
            Location = new Point(10, 88),
            AutoSize = true,
            Checked = false
        };

        trackChannelSingleCheckBox = new CheckBox
        {
            Text = "Track channel",
            Location = new Point(380, 88),
            AutoSize = true,
            Checked = false
        };

        singleTab.Controls.Add(urlLabel);
        singleTab.Controls.Add(singlePasteButton);
        singleTab.Controls.Add(urlTextBox);
        singleTab.Controls.Add(goButton);
        singleTab.Controls.Add(singleFolderLabel);
        singleTab.Controls.Add(singleFolderComboBox);
        singleTab.Controls.Add(singleClearFolderButton);
        singleTab.Controls.Add(allowRenameCheckBox);
        singleTab.Controls.Add(trackChannelSingleCheckBox);

        downloadTabControl.TabPages.Add(singleTab);
    }

    private void InitializePlaylistTab()
    {
        var playlistTab = new TabPage("Playlist");

        var playlistUrlLabel = new Label
        {
            Text = "Playlist URL:",
            Location = new Point(10, 12),
            AutoSize = true
        };

        var playlistPasteButton = new Button
        {
            Text = "Paste",
            Location = new Point(80, 8),
            Size = new Size(50, 23)
        };
        playlistPasteButton.Click += (s, e) =>
        {
            if (Clipboard.ContainsText())
            {
                playlistUrlTextBox.Text = Clipboard.GetText().Trim();
            }
        };

        playlistUrlTextBox = new TextBox
        {
            Location = new Point(10, 32),
            Size = new Size(430, 23)
        };
        playlistUrlTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PlaylistGoButton_Click(s, e);
            }
        };
        playlistUrlTextBox.TextChanged += PlaylistUrlTextBox_TextChanged;

        playlistBrowseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(445, 31),
            Size = new Size(75, 25),
            Enabled = false  // Disabled until URL is entered
        };
        playlistBrowseButton.Click += PlaylistBrowseButton_Click;

        var playlistFolderLabel = new Label
        {
            Text = "Folder Name:",
            Location = new Point(10, 65),
            AutoSize = true
        };

        playlistFolderComboBox = new ComboBox
        {
            Location = new Point(95, 62),
            Size = new Size(345, 23),
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        playlistFolderComboBox.TextChanged += PlaylistFolderComboBox_TextChanged;

        playlistGoButton = new Button
        {
            Text = "Go",
            Location = new Point(445, 61),
            Size = new Size(75, 25),
            Enabled = false
        };
        playlistGoButton.Click += PlaylistGoButton_Click;

        trackEachSongCheckBox = new CheckBox
        {
            Text = "Track each song separately in history",
            Location = new Point(10, 88),
            AutoSize = true,
            Checked = false
        };

        trackChannelPlaylistCheckBox = new CheckBox
        {
            Text = "Track channel",
            Location = new Point(270, 88),
            AutoSize = true,
            Checked = false
        };

        playlistTab.Controls.Add(playlistUrlLabel);
        playlistTab.Controls.Add(playlistPasteButton);
        playlistTab.Controls.Add(playlistUrlTextBox);
        playlistTab.Controls.Add(playlistBrowseButton);
        playlistTab.Controls.Add(playlistFolderLabel);
        playlistTab.Controls.Add(playlistFolderComboBox);
        playlistTab.Controls.Add(playlistGoButton);
        playlistTab.Controls.Add(trackEachSongCheckBox);
        playlistTab.Controls.Add(trackChannelPlaylistCheckBox);

        downloadTabControl.TabPages.Add(playlistTab);
    }

    private void InitializeProgressSection(ref int y)
    {
        // Download Progress Bar (hidden until download starts)
        downloadLabel = new Label
        {
            Text = "Download:",
            Location = new Point(12, y + 2),
            AutoSize = true,
            Visible = false
        };

        downloadProgressBar = new ProgressBar
        {
            Location = new Point(85, y),
            Size = new Size(460, 20),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };
        y += 28;

        // Convert Progress Bar (hidden until download starts)
        convertLabel = new Label
        {
            Text = "Convert:",
            Location = new Point(12, y + 2),
            AutoSize = true,
            Visible = false
        };

        convertProgressBar = new ProgressBar
        {
            Location = new Point(85, y),
            Size = new Size(460, 20),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };
        y += 28;

        // Playlist Progress Bar (for overall playlist progress)
        playlistLabel = new Label
        {
            Text = "Playlist:",
            Location = new Point(12, y + 2),
            AutoSize = true,
            Visible = false
        };

        playlistProgressBar = new ProgressBar
        {
            Location = new Point(85, y),
            Size = new Size(460, 20),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };

        // Overlay label to show "X/Y" text centered on the playlist progress bar
        playlistProgressLabel = new Label
        {
            Text = "",
            Location = new Point(85, y),
            Size = new Size(460, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Visible = false
        };
        y += 35;
    }

    private void InitializeStatusSection(ref int y)
    {
        // Status Label
        statusLabel = new Label
        {
            Text = "Ready",
            Location = new Point(12, y),
            Size = new Size(533, 20),
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 25;

        // Source Link (clickable URL, "Source:" prefix not clickable)
        sourceLink = new LinkLabel
        {
            Text = "",
            Location = new Point(12, y),
            Size = new Size(533, 36),
            AutoSize = false,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        sourceLink.LinkClicked += SourceLink_Click;
        y += 40;

        // Destination Link ("Saved to:" prefix not clickable, path is clickable)
        destinationLink = new LinkLabel
        {
            Text = "",
            Location = new Point(12, y),
            Size = new Size(470, 36),
            AutoSize = false,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        destinationLink.LinkClicked += DestinationLink_Click;

        // Play Song button (visible only after single video download)
        playSongButton = new Button
        {
            Text = "Play",
            Location = new Point(485, y + 5),
            Size = new Size(55, 25),
            Visible = false
        };
        playSongButton.Click += PlaySongButton_Click;
        y += 40;
    }

    private void InitializeButtons(ref int y)
    {
        // Cancel Button (hidden until processing)
        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(463, y),
            Size = new Size(75, 25),
            Visible = false
        };
        cancelButton.Click += CancelButton_Click;

        // Clear All button to reset all input fields (below Cancel)
        clearAllButton = new Button
        {
            Text = "Clear All",
            Location = new Point(463, y + 30),
            Size = new Size(75, 25),
            BackColor = SystemColors.Control,
            UseVisualStyleBackColor = true
        };
        clearAllButton.Click += ClearAllButton_Click;
    }

    private void InitializeStatusStrip()
    {
        // Status bar at bottom for timing info
        statusStrip = new StatusStrip();
        downloadTimeLabel = new ToolStripStatusLabel
        {
            Text = "DL: --:--",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            AutoSize = false,
            Width = 70
        };
        convertTimeLabel = new ToolStripStatusLabel
        {
            Text = "Conv: --:--",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            AutoSize = false,
            Width = 75
        };
        totalTimeLabel = new ToolStripStatusLabel
        {
            Text = "Total: --:--",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            AutoSize = false,
            Width = 80
        };
        cpuLabel = new ToolStripStatusLabel
        {
            Text = "CPU: --%",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            AutoSize = false,
            Width = 65
        };
        memoryLabel = new ToolStripStatusLabel
        {
            Text = "Mem: --%",
            BorderSides = ToolStripStatusLabelBorderSides.Right,
            AutoSize = false,
            Width = 70
        };
        driveLabel = new ToolStripStatusLabel
        {
            Text = "C:\\ --% free",
            AutoSize = false,
            Width = 95,
            BorderSides = ToolStripStatusLabelBorderSides.Right
        };
        var springLabel = new ToolStripStatusLabel
        {
            Spring = true // Takes up remaining space
        };
        youtubeStatusLabel = new ToolStripStatusLabel
        {
            Text = "YouTube: Not signed in",
            ForeColor = Color.Gray,
            AutoSize = true
        };
        statusStrip.Items.Add(downloadTimeLabel);
        statusStrip.Items.Add(convertTimeLabel);
        statusStrip.Items.Add(totalTimeLabel);
        statusStrip.Items.Add(cpuLabel);
        statusStrip.Items.Add(memoryLabel);
        statusStrip.Items.Add(driveLabel);
        statusStrip.Items.Add(springLabel);
        statusStrip.Items.Add(youtubeStatusLabel);
    }

    // Height of media player panel - used for expanding form
    private const int MediaPlayerPanelHeight = 75;

    private void InitializeMediaPlayerSection()
    {
        // Media player panel (hidden by default, shown when music is playing)
        // Positioned below Clear All button, form expands when shown
        mediaPlayerPanel = new Panel
        {
            Location = new Point(12, 400),  // Will be adjusted when shown
            Size = new Size(540, MediaPlayerPanelHeight),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false  // Hidden until a track plays
        };

        // Album art thumbnail on the left
        playerAlbumArt = new SafePictureBox
        {
            Location = new Point(5, 5),
            Size = new Size(65, 65),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        playerTitleLabel = new Label
        {
            Text = "No track loaded",
            Location = new Point(78, 5),
            Size = new Size(290, 20),
            AutoEllipsis = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };

        playerTimeLabel = new Label
        {
            Text = "0:00 / 0:00",
            Location = new Point(375, 5),
            Size = new Size(80, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        playerPlayPauseButton = new Button
        {
            Text = "Pause",
            Location = new Point(460, 3),
            Size = new Size(70, 23),
            Enabled = false
        };
        playerPlayPauseButton.Click += MainPlayerPlayPauseButton_Click;

        playerSeekBar = new TrackBar
        {
            Location = new Point(78, 23),
            Size = new Size(378, 25),
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.None,
            Enabled = false
        };
        playerSeekBar.Scroll += MainPlayerSeekBar_Scroll;
        playerSeekBar.MouseDown += MainPlayerSeekBar_MouseDown;

        playerStopButton = new Button
        {
            Text = "Stop",
            Location = new Point(460, 28),
            Size = new Size(70, 23),
            Enabled = false
        };
        playerStopButton.Click += MainPlayerStopButton_Click;

        var volumeLabel = new Label
        {
            Text = "Vol:",
            Location = new Point(78, 50),
            Size = new Size(28, 18)
        };

        playerVolumeBar = new TrackBar
        {
            Location = new Point(105, 45),
            Size = new Size(120, 25),
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            TickStyle = TickStyle.None
        };
        playerVolumeBar.Scroll += MainPlayerVolumeBar_Scroll;

        mediaPlayerPanel.Controls.Add(playerAlbumArt);
        mediaPlayerPanel.Controls.Add(playerTitleLabel);
        mediaPlayerPanel.Controls.Add(playerTimeLabel);
        mediaPlayerPanel.Controls.Add(playerPlayPauseButton);
        mediaPlayerPanel.Controls.Add(playerSeekBar);
        mediaPlayerPanel.Controls.Add(playerStopButton);
        mediaPlayerPanel.Controls.Add(volumeLabel);
        mediaPlayerPanel.Controls.Add(playerVolumeBar);
    }

    private void InitializeSharedMediaPlayerEvents()
    {
        // Subscribe to shared media player events
        var player = SharedMediaPlayer.Instance;
        player.PositionChanged += MainPlayer_PositionChanged;
        player.PlaybackStopped += MainPlayer_PlaybackStopped;
        player.TrackEnded += MainPlayer_TrackEnded;
        SharedMediaPlayer.TrackStarted += MainPlayer_TrackStarted;
        SharedMediaPlayer.Stopped += MainPlayer_Stopped;
    }

    private void MainPlayer_Stopped(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MainPlayer_Stopped(sender, e));
            return;
        }

        UpdateMainPlayerUI();
    }

    private bool mediaPlayerShown = false;

    private void MainPlayer_TrackStarted(object? sender, TrackStartedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MainPlayer_TrackStarted(sender, e));
            return;
        }

        // Show the media player panel and expand form
        if (!mediaPlayerShown)
        {
            ShowMediaPlayerPanel();
            mediaPlayerShown = true;
        }

        // Update UI
        if (playerTitleLabel != null)
            playerTitleLabel.Text = e.Title;
        if (playerPlayPauseButton != null)
        {
            playerPlayPauseButton.Enabled = true;
            playerPlayPauseButton.Text = "Pause";
        }
        if (playerStopButton != null)
            playerStopButton.Enabled = true;
        if (playerSeekBar != null)
            playerSeekBar.Enabled = true;

        // Load album art
        LoadMainPlayerAlbumArt(e.FilePath);
    }

    private void ShowMediaPlayerPanel()
    {
        if (mediaPlayerPanel == null) return;

        // Remember which form had focus so we don't steal it
        var activeForm = Form.ActiveForm;

        // Position the panel below the Clear All button
        int panelY = clearAllButton.Bottom + 10;
        mediaPlayerPanel.Location = new Point(12, panelY);

        // Expand the form to make room
        this.Height += MediaPlayerPanelHeight;
        this.MinimumSize = new Size(500, 510 + MediaPlayerPanelHeight);

        mediaPlayerPanel.Visible = true;

        // Restore focus to the previously active form
        if (activeForm != null && activeForm != this && !activeForm.IsDisposed)
        {
            activeForm.Activate();
        }
    }

    private void HideMediaPlayerPanel()
    {
        if (mediaPlayerPanel == null) return;

        mediaPlayerPanel.Visible = false;

        // Shrink the form back
        this.MinimumSize = new Size(500, 510);
        this.Height -= MediaPlayerPanelHeight;
    }

    private void MainPlayer_PositionChanged(object? sender, TimeSpan position)
    {
        if (InvokeRequired)
        {
            Invoke(() => MainPlayer_PositionChanged(sender, position));
            return;
        }

        var player = SharedMediaPlayer.Instance;
        var duration = player.Duration;

        if (playerTimeLabel != null)
            playerTimeLabel.Text = $"{FormatPlayerTimeSpan(position)} / {FormatPlayerTimeSpan(duration)}";

        if (playerSeekBar != null && duration.TotalSeconds > 0)
        {
            int seekPosition = (int)(position.TotalSeconds / duration.TotalSeconds * 1000);
            playerSeekBar.Value = Math.Min(seekPosition, 1000);
        }
    }

    private void MainPlayer_PlaybackStopped(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MainPlayer_PlaybackStopped(sender, e));
            return;
        }

        UpdateMainPlayerUI();
    }

    private void MainPlayer_TrackEnded(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MainPlayer_TrackEnded(sender, e));
            return;
        }

        SharedMediaPlayer.Stop();
        UpdateMainPlayerUI();
    }

    private void MainPlayerPlayPauseButton_Click(object? sender, EventArgs e)
    {
        var player = SharedMediaPlayer.Instance;

        if (player.IsPlaying)
        {
            player.Pause();
            if (playerPlayPauseButton != null)
                playerPlayPauseButton.Text = "Play";
        }
        else if (player.IsPaused)
        {
            player.Resume();
            if (playerPlayPauseButton != null)
                playerPlayPauseButton.Text = "Pause";
        }
    }

    private void MainPlayerStopButton_Click(object? sender, EventArgs e)
    {
        SharedMediaPlayer.Stop();
        UpdateMainPlayerUI();
    }

    private void MainPlayerSeekBar_Scroll(object? sender, EventArgs e)
    {
        var player = SharedMediaPlayer.Instance;
        if (player.HasTrack && playerSeekBar != null)
        {
            double percent = playerSeekBar.Value / 1000.0;
            player.SeekPercent(percent);
        }
    }

    private void MainPlayerSeekBar_MouseDown(object? sender, MouseEventArgs e)
    {
        var player = SharedMediaPlayer.Instance;
        if (!player.HasTrack || playerSeekBar == null) return;

        // Calculate the position based on click location
        // TrackBar has some padding on left/right for the thumb
        const int thumbHalfWidth = 5;
        int trackWidth = playerSeekBar.Width - (2 * thumbHalfWidth);
        int clickPos = e.X - thumbHalfWidth;

        // Clamp to valid range
        clickPos = Math.Max(0, Math.Min(clickPos, trackWidth));

        // Calculate percentage and new value
        double percent = (double)clickPos / trackWidth;
        int newValue = (int)(percent * (playerSeekBar.Maximum - playerSeekBar.Minimum)) + playerSeekBar.Minimum;

        // Set the value and seek
        playerSeekBar.Value = Math.Max(playerSeekBar.Minimum, Math.Min(newValue, playerSeekBar.Maximum));
        player.SeekPercent(playerSeekBar.Value / 1000.0);
    }

    private void MainPlayerVolumeBar_Scroll(object? sender, EventArgs e)
    {
        if (playerVolumeBar != null)
        {
            SharedMediaPlayer.Instance.Volume = playerVolumeBar.Value / 100f;
        }
    }

    private void UpdateMainPlayerUI()
    {
        var player = SharedMediaPlayer.Instance;
        bool hasTrack = player.HasTrack;

        if (playerPlayPauseButton != null)
        {
            playerPlayPauseButton.Enabled = hasTrack;
            playerPlayPauseButton.Text = player.IsPlaying ? "Pause" : "Play";
        }
        if (playerStopButton != null)
            playerStopButton.Enabled = hasTrack;
        if (playerSeekBar != null)
            playerSeekBar.Enabled = hasTrack;

        if (!hasTrack)
        {
            // Hide the panel and shrink form when no track is loaded
            if (mediaPlayerShown)
            {
                HideMediaPlayerPanel();
                mediaPlayerShown = false;
            }

            if (playerTitleLabel != null)
                playerTitleLabel.Text = "No track loaded";
            if (playerTimeLabel != null)
                playerTimeLabel.Text = "0:00 / 0:00";
            if (playerSeekBar != null)
                playerSeekBar.Value = 0;
            if (playerAlbumArt != null)
            {
                playerAlbumArt.Image?.Dispose();
                playerAlbumArt.Image = null;
            }
        }
    }

    private void LoadMainPlayerAlbumArt(string filePath)
    {
        if (playerAlbumArt == null) return;

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var pictures = tagFile.Tag.Pictures;
            if (pictures.Length > 0)
            {
                // Create a copy so we don't depend on the stream staying open
                using var ms = new System.IO.MemoryStream(pictures[0].Data.Data);
                using var tempImage = Image.FromStream(ms);
                playerAlbumArt.Image?.Dispose();
                playerAlbumArt.Image = new Bitmap(tempImage);
            }
            else
            {
                playerAlbumArt.Image?.Dispose();
                playerAlbumArt.Image = null;
            }
        }
        catch
        {
            playerAlbumArt.Image?.Dispose();
            playerAlbumArt.Image = null;
        }
    }

    private static string FormatPlayerTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
