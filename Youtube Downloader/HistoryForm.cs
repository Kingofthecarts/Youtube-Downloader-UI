using System.Diagnostics;

namespace Youtube_Downloader;

public class HistoryForm : Form
{
    private TabControl tabControl = null!;
    private DataGridView singlesGrid = null!;
    private DataGridView playlistsGrid = null!;
    private DataGridView tracksGrid = null!;
    private DataGridView statsGrid = null!;
    private TextBox searchBox = null!;
    private Label tracksLabel = null!;
    private Label statsSummaryLabel = null!;
    private CheckBox showExcludedCheckBox = null!;
    private readonly DownloadHistory history;
    private readonly DownloadStats stats;
    private readonly MainForm mainForm;
    private string currentSearchText = "";
    private bool showExcluded = false;
    private DownloadRecord? currentPlaylist = null;

    // Cached data to avoid reloading from XML
    private List<DownloadRecord> cachedSingles = new();
    private List<DownloadRecord> cachedPlaylists = new();

    public HistoryForm(DownloadHistory history, DownloadStats stats, MainForm mainForm)
    {
        this.history = history;
        this.stats = stats;
        this.mainForm = mainForm;
        InitializeComponents();
        CacheData();
        RefreshDisplay();
    }

    private void CacheData()
    {
        // Load data once from XML into memory
        cachedSingles = history.SingleRecords.ToList();
        cachedPlaylists = history.PlaylistRecords.ToList();
    }

    private void InitializeComponents()
    {
        Text = "Download History";
        Size = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(800, 500);

        // Tab Control
        tabControl = new TabControl
        {
            Location = new Point(12, 12),
            Size = new Size(960, 500),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Singles Tab
        var singlesTab = new TabPage("Singles");
        singlesGrid = CreateSinglesGrid();
        singlesTab.Controls.Add(singlesGrid);
        tabControl.TabPages.Add(singlesTab);

        // Playlists Tab
        var playlistsTab = new TabPage("Playlists");

        // Split container for playlists and tracks
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 200,
            Panel1MinSize = 100,
            Panel2MinSize = 100
        };

        playlistsGrid = CreatePlaylistsGrid();
        playlistsGrid.Dock = DockStyle.Fill;
        playlistsGrid.SelectionChanged += PlaylistsGrid_SelectionChanged;
        splitContainer.Panel1.Controls.Add(playlistsGrid);

        // Tracks panel
        var tracksPanel = new Panel { Dock = DockStyle.Fill };

        tracksLabel = new Label
        {
            Text = "Select a playlist to view tracks",
            Location = new Point(5, 5),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };

        tracksGrid = CreateTracksGrid();
        tracksGrid.Location = new Point(0, 25);
        tracksGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        tracksPanel.Controls.Add(tracksLabel);
        tracksPanel.Controls.Add(tracksGrid);
        tracksPanel.Resize += (s, e) =>
        {
            tracksGrid.Size = new Size(tracksPanel.Width, tracksPanel.Height - 30);
        };

        splitContainer.Panel2.Controls.Add(tracksPanel);
        playlistsTab.Controls.Add(splitContainer);
        tabControl.TabPages.Add(playlistsTab);

        // Stats Tab
        var statsTab = new TabPage("Stats");
        var statsPanel = new Panel { Dock = DockStyle.Fill };

        statsSummaryLabel = new Label
        {
            Location = new Point(10, 10),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
        };

        statsGrid = CreateStatsGrid();
        statsGrid.Location = new Point(0, 50);
        statsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        statsPanel.Controls.Add(statsSummaryLabel);
        statsPanel.Controls.Add(statsGrid);
        statsPanel.Resize += (s, e) =>
        {
            statsGrid.Size = new Size(statsPanel.Width, statsPanel.Height - 60);
        };

        statsTab.Controls.Add(statsPanel);
        tabControl.TabPages.Add(statsTab);

        // Search section
        var searchLabel = new Label
        {
            Text = "Search:",
            Location = new Point(12, 525),
            Size = new Size(50, 23),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };

        searchBox = new TextBox
        {
            Location = new Point(65, 525),
            Size = new Size(300, 23),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        var clearSearchButton = new Button
        {
            Text = "Clear",
            Location = new Point(375, 524),
            Size = new Size(60, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        clearSearchButton.Click += (s, e) => { searchBox.Clear(); };

        showExcludedCheckBox = new CheckBox
        {
            Text = "Show excluded/superseded",
            Location = new Point(450, 526),
            Size = new Size(180, 20),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Checked = false
        };
        showExcludedCheckBox.CheckedChanged += ShowExcludedCheckBox_CheckedChanged;

        // Buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Location = new Point(12, 560),
            Size = new Size(960, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.RightToLeft
        };

        var closeButton = new Button
        {
            Text = "Close",
            Size = new Size(80, 28)
        };
        closeButton.Click += (s, e) => Close();

        var clearButton = new Button
        {
            Text = "Clear Tab",
            Size = new Size(80, 28)
        };
        clearButton.Click += ClearButton_Click;

        var openFolderButton = new Button
        {
            Text = "Open Location",
            Size = new Size(100, 28)
        };
        openFolderButton.Click += OpenFolderButton_Click;

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(clearButton);
        buttonPanel.Controls.Add(openFolderButton);

        Controls.Add(tabControl);
        Controls.Add(searchLabel);
        Controls.Add(searchBox);
        Controls.Add(clearSearchButton);
        Controls.Add(showExcludedCheckBox);
        Controls.Add(buttonPanel);
    }

    private DataGridView CreateSinglesGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", Width = 250 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File Name", Width = 130 });
        grid.Columns.Add(new DataGridViewLinkColumn { Name = "Channel", HeaderText = "Channel", Width = 100, TrackVisitedState = false });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "MonitorChannel", HeaderText = "", Text = "+", UseColumnTextForButtonValue = true, Width = 25, ToolTipText = "Monitor this channel" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "VideoId", HeaderText = "Video ID", Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Size", Width = 70 });
        grid.Columns.Add(new DataGridViewLinkColumn { Name = "Url", HeaderText = "URL", Width = 180, TrackVisitedState = false });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "Play", HeaderText = "", Text = "Play", UseColumnTextForButtonValue = true, Width = 45 });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "X", UseColumnTextForButtonValue = true, Width = 30 });

        grid.CellDoubleClick += SinglesGrid_CellDoubleClick;
        grid.CellContentClick += SinglesGrid_CellContentClick;

        return grid;
    }

    private DataGridView CreatePlaylistsGrid()
    {
        var grid = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Playlist Title", Width = 300 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tracks", HeaderText = "Tracks", Width = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Total Size", Width = 80 });
        grid.Columns.Add(new DataGridViewLinkColumn { Name = "Url", HeaderText = "Playlist URL", Width = 300, TrackVisitedState = false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Folder", HeaderText = "Folder", Width = 200 });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "X", UseColumnTextForButtonValue = true, Width = 30 });

        grid.CellContentClick += PlaylistsGrid_CellContentClick;

        return grid;
    }

    private DataGridView CreateTracksGrid()
    {
        var grid = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "#", Width = 40 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Track Title", Width = 200 });
        grid.Columns.Add(new DataGridViewLinkColumn { Name = "Channel", HeaderText = "Channel", Width = 100, TrackVisitedState = false });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "MonitorChannel", HeaderText = "", Text = "+", UseColumnTextForButtonValue = true, Width = 25, ToolTipText = "Monitor this channel" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File Name", Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Size", Width = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Folder", HeaderText = "Folder", Width = 150 });
        grid.Columns.Add(new DataGridViewLinkColumn { Name = "Url", HeaderText = "Video URL", Width = 180, TrackVisitedState = false });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "Play", HeaderText = "", Text = "Play", UseColumnTextForButtonValue = true, Width = 45 });
        grid.Columns.Add(new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "X", UseColumnTextForButtonValue = true, Width = 30 });

        grid.CellContentClick += TracksGrid_CellContentClick;

        return grid;
    }

    private DataGridView CreateStatsGrid()
    {
        var grid = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DayOfWeek", HeaderText = "Day", Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SongCount", HeaderText = "Songs", Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSize", HeaderText = "Total Size", Width = 100 });

        return grid;
    }

    private void RefreshDisplay()
    {
        RefreshSinglesDisplay();
        RefreshPlaylistsDisplay();
        RefreshStatsDisplay();
    }

    private void RefreshStatsDisplay()
    {
        statsGrid.Rows.Clear();

        var allStats = stats.GetAllStats();

        foreach (var day in allStats)
        {
            statsGrid.Rows.Add(
                day.Date.ToString("yyyy-MM-dd"),
                day.Date.ToString("dddd"),
                day.SongCount.ToString(),
                day.TotalSizeFormatted
            );
        }

        // Update summary label
        int totalSongs = stats.GetTotalSongCount();
        string totalSize = stats.GetTotalSizeFormatted();
        int daysWithDownloads = stats.GetDaysWithDownloads();

        var todayStats = stats.GetTodayStats();
        string todayText = todayStats != null
            ? $"Today: {todayStats.SongCount} songs ({todayStats.TotalSizeFormatted})"
            : "Today: 0 songs";

        statsSummaryLabel.Text = $"Total: {totalSongs} songs ({totalSize}) over {daysWithDownloads} days  |  {todayText}";
    }

    private void RefreshSinglesDisplay()
    {
        singlesGrid.Rows.Clear();

        foreach (var record in cachedSingles)
        {
            if (!MatchesSearch(record)) continue;

            // Hide superseded entries unless checkbox is checked
            if (record.IsSuperseded && !showExcluded) continue;

            var rowIndex = singlesGrid.Rows.Add(
                record.DownloadDate.ToString("yyyy-MM-dd HH:mm"),
                record.Title,
                record.FileName,
                string.IsNullOrEmpty(record.ChannelName) ? "" : record.ChannelName,
                "+",
                record.VideoId,
                record.FileSizeFormatted,
                record.Url,
                "Play",
                "X"
            );

            singlesGrid.Rows[rowIndex].Tag = record;

            // Disable MonitorChannel button if channel is already monitored or no channel info
            if (!string.IsNullOrEmpty(record.ChannelId))
            {
                if (mainForm.IsChannelMonitored(record.ChannelId))
                {
                    singlesGrid.Rows[rowIndex].Cells["MonitorChannel"].Value = "";
                    singlesGrid.Rows[rowIndex].Cells["MonitorChannel"].Style.ForeColor = Color.Gray;
                }
            }
            else
            {
                singlesGrid.Rows[rowIndex].Cells["MonitorChannel"].Value = "";
            }

            if (record.IsSuperseded)
            {
                singlesGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
            }

            if (!string.IsNullOrEmpty(currentSearchText))
            {
                HighlightMatchingCells(singlesGrid.Rows[rowIndex], record);
            }
        }
    }

    private void RefreshPlaylistsDisplay()
    {
        playlistsGrid.Rows.Clear();
        tracksGrid.Rows.Clear();
        tracksLabel.Text = "Select a playlist to view tracks";

        foreach (var record in cachedPlaylists)
        {
            if (!MatchesSearch(record)) continue;

            // Hide superseded entries unless checkbox is checked
            if (record.IsSuperseded && !showExcluded) continue;

            var rowIndex = playlistsGrid.Rows.Add(
                record.DownloadDate.ToString("yyyy-MM-dd HH:mm"),
                record.Title,
                record.PlaylistItemCount.ToString(),
                record.FileSizeFormatted,
                record.Url,
                record.DownloadFolder,
                "X"
            );

            playlistsGrid.Rows[rowIndex].Tag = record;

            if (record.IsSuperseded)
            {
                playlistsGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
            }
        }
    }

    private void RefreshTracksDisplay(DownloadRecord playlist)
    {
        currentPlaylist = playlist;
        tracksGrid.Rows.Clear();

        int downloadedCount = playlist.PlaylistTracks.Count(t => !t.IsExcluded);
        int excludedCount = playlist.PlaylistTracks.Count(t => t.IsExcluded);

        if (excludedCount > 0)
        {
            string excludedInfo = showExcluded ? $", {excludedCount} excluded shown" : $", {excludedCount} excluded hidden";
            tracksLabel.Text = $"Tracks in: {playlist.Title} ({downloadedCount} downloaded{excludedInfo})";
        }
        else
        {
            tracksLabel.Text = $"Tracks in: {playlist.Title} ({playlist.PlaylistTracks.Count} tracks)";
        }

        foreach (var track in playlist.PlaylistTracks)
        {
            // Hide excluded tracks unless checkbox is checked
            if (track.IsExcluded && !showExcluded) continue;

            // Use PlaylistIndex if available, otherwise use position in list
            string indexText = track.PlaylistIndex > 0 ? track.PlaylistIndex.ToString() : "";
            string dateText = track.DownloadDate != DateTime.MinValue
                ? track.DownloadDate.ToString("yyyy-MM-dd HH:mm")
                : "";

            int rowIndex = tracksGrid.Rows.Add(
                indexText,
                dateText,
                track.Title,
                string.IsNullOrEmpty(track.ChannelName) ? "" : track.ChannelName,
                "+",
                track.FileName,
                track.IsExcluded ? "" : track.FileSizeFormatted,
                track.Folder,
                track.Url,
                "Play",
                "X"
            );

            // Store track for channel link
            tracksGrid.Rows[rowIndex].Tag = track;

            // Disable MonitorChannel button if channel is already monitored or no channel info
            if (!string.IsNullOrEmpty(track.ChannelId))
            {
                if (mainForm.IsChannelMonitored(track.ChannelId))
                {
                    tracksGrid.Rows[rowIndex].Cells["MonitorChannel"].Value = "";
                    tracksGrid.Rows[rowIndex].Cells["MonitorChannel"].Style.ForeColor = Color.Gray;
                }
            }
            else
            {
                tracksGrid.Rows[rowIndex].Cells["MonitorChannel"].Value = "";
            }

            // Color excluded tracks red
            if (track.IsExcluded)
            {
                tracksGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                tracksGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkRed;
            }
        }
    }

    private bool MatchesSearch(DownloadRecord record)
    {
        if (string.IsNullOrEmpty(currentSearchText)) return true;

        return record.Title.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               record.VideoId.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               record.Url.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               record.DownloadFolder.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               record.PlaylistTracks.Any(t =>
                   t.Title.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                   t.FileName.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase));
    }

    private void HighlightMatchingCells(DataGridViewRow row, DownloadRecord record)
    {
        if (record.Title.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Title"].Style.BackColor = Color.Yellow;

        if (record.VideoId.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["VideoId"].Style.BackColor = Color.Yellow;

        if (record.Url.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Url"].Style.BackColor = Color.Yellow;
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        currentSearchText = searchBox.Text.Trim();
        RefreshDisplay();
    }

    private void ShowExcludedCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        showExcluded = showExcludedCheckBox.Checked;
        RefreshDisplay();

        // Also refresh tracks if a playlist is selected
        if (playlistsGrid.SelectedRows.Count > 0)
        {
            var record = playlistsGrid.SelectedRows[0].Tag as DownloadRecord;
            if (record != null)
            {
                RefreshTracksDisplay(record);
            }
        }
    }

    private void PlaylistsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (playlistsGrid.SelectedRows.Count > 0)
        {
            var record = playlistsGrid.SelectedRows[0].Tag as DownloadRecord;
            if (record != null)
            {
                RefreshTracksDisplay(record);
            }
        }
    }

    private void SinglesGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (singlesGrid.Columns[e.ColumnIndex].Name == "Url")
        {
            var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null) OpenUrl(record.Url);
        }

        if (singlesGrid.Columns[e.ColumnIndex].Name == "Channel")
        {
            var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null && !string.IsNullOrEmpty(record.ChannelUrl))
            {
                OpenUrl(record.ChannelUrl);
            }
        }

        if (singlesGrid.Columns[e.ColumnIndex].Name == "MonitorChannel")
        {
            var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null && !string.IsNullOrEmpty(record.ChannelId) && !string.IsNullOrEmpty(record.ChannelUrl))
            {
                _ = mainForm.AddChannelToMonitorAsync(record.ChannelId, record.ChannelName, record.ChannelUrl);
            }
            else
            {
                MessageBox.Show("Channel information not available for this video.",
                    "No Channel Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        if (singlesGrid.Columns[e.ColumnIndex].Name == "Play")
        {
            var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null) PlaySongFromRecord(record);
        }

        if (singlesGrid.Columns[e.ColumnIndex].Name == "Delete")
        {
            var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null) DeleteRecord(record);
        }
    }

    private void PlaySongFromRecord(DownloadRecord record)
    {
        // Check if the file still exists at the expected path
        if (File.Exists(record.FilePath))
        {
            // Open the song browser and play the song
            mainForm.OpenSongBrowserAndPlay(record.FilePath);
            return;
        }

        // File not found - try to find by video ID in comments
        if (!string.IsNullOrEmpty(record.VideoId))
        {
            if (mainForm.OpenSongBrowserAndPlayByVideoId(record.VideoId))
            {
                // Found and playing by video ID
                return;
            }
        }

        // Could not find by either method
        MessageBox.Show(
            $"Song not found. The file may have been deleted or renamed.\n\nExpected path: {record.FilePath}",
            "Song Not Found",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void SinglesGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || singlesGrid.Columns[e.ColumnIndex].Name == "Url") return;

        var record = singlesGrid.Rows[e.RowIndex].Tag as DownloadRecord;
        if (record != null && File.Exists(record.FilePath))
        {
            Process.Start("explorer.exe", $"/select,\"{record.FilePath}\"");
        }
    }

    private void PlaylistsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (playlistsGrid.Columns[e.ColumnIndex].Name == "Url")
        {
            var record = playlistsGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null) OpenUrl(record.Url);
        }

        if (playlistsGrid.Columns[e.ColumnIndex].Name == "Delete")
        {
            var record = playlistsGrid.Rows[e.RowIndex].Tag as DownloadRecord;
            if (record != null) DeleteRecord(record);
        }
    }

    private void TracksGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (tracksGrid.Columns[e.ColumnIndex].Name == "Url")
        {
            var url = tracksGrid.Rows[e.RowIndex].Cells["Url"].Value?.ToString();
            if (!string.IsNullOrEmpty(url)) OpenUrl(url);
        }

        if (tracksGrid.Columns[e.ColumnIndex].Name == "Channel")
        {
            var track = tracksGrid.Rows[e.RowIndex].Tag as PlaylistTrack;
            if (track != null && !string.IsNullOrEmpty(track.ChannelUrl))
            {
                OpenUrl(track.ChannelUrl);
            }
        }

        if (tracksGrid.Columns[e.ColumnIndex].Name == "MonitorChannel")
        {
            var track = tracksGrid.Rows[e.RowIndex].Tag as PlaylistTrack;
            if (track != null && !string.IsNullOrEmpty(track.ChannelId) && !string.IsNullOrEmpty(track.ChannelUrl))
            {
                _ = mainForm.AddChannelToMonitorAsync(track.ChannelId, track.ChannelName, track.ChannelUrl);
            }
            else
            {
                MessageBox.Show("Channel information not available for this track.",
                    "No Channel Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        if (tracksGrid.Columns[e.ColumnIndex].Name == "Play")
        {
            var track = tracksGrid.Rows[e.RowIndex].Tag as PlaylistTrack;
            if (track != null) PlayTrackFromRecord(track);
        }

        if (tracksGrid.Columns[e.ColumnIndex].Name == "Delete")
        {
            var track = tracksGrid.Rows[e.RowIndex].Tag as PlaylistTrack;
            if (track != null) DeleteTrackFromPlaylist(track);
        }
    }

    private void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    private void PlayTrackFromRecord(PlaylistTrack track)
    {
        // Build the full file path from folder and filename
        string filePath = Path.Combine(track.Folder, track.FileName);

        // Check if the file still exists at the expected path
        if (File.Exists(filePath))
        {
            // Open the song browser and play the song
            mainForm.OpenSongBrowserAndPlay(filePath);
            return;
        }

        // File not found - try to find by video ID in comments
        if (!string.IsNullOrEmpty(track.VideoId))
        {
            if (mainForm.OpenSongBrowserAndPlayByVideoId(track.VideoId))
            {
                // Found and playing by video ID
                return;
            }
        }

        // Could not find by either method
        MessageBox.Show(
            $"Song not found. The file may have been deleted or renamed.\n\nExpected path: {filePath}",
            "Song Not Found",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void DeleteTrackFromPlaylist(PlaylistTrack track)
    {
        if (currentPlaylist == null) return;

        var result = MessageBox.Show(
            $"Delete this track from the playlist history?\n\n{track.Title}\n\nThis will not delete the downloaded file.",
            "Delete Track",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            currentPlaylist.PlaylistTracks.Remove(track);
            history.Save();
            CacheData(); // Reload from XML after deletion
            RefreshTracksDisplay(currentPlaylist);
        }
    }

    private void DeleteRecord(DownloadRecord record)
    {
        var result = MessageBox.Show(
            $"Delete this entry from history?\n\n{record.Title}\n\nThis will not delete the downloaded files.",
            "Delete Entry",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            history.DeleteRecord(record);
            CacheData(); // Reload from XML after deletion
            RefreshDisplay();
        }
    }

    private void OpenFolderButton_Click(object? sender, EventArgs e)
    {
        DataGridView currentGrid = tabControl.SelectedIndex == 0 ? singlesGrid : playlistsGrid;

        if (currentGrid.SelectedRows.Count == 0) return;

        var record = currentGrid.SelectedRows[0].Tag as DownloadRecord;
        if (record != null)
        {
            if (File.Exists(record.FilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{record.FilePath}\"");
            }
            else if (Directory.Exists(record.DownloadFolder))
            {
                Process.Start("explorer.exe", record.DownloadFolder);
            }
            else
            {
                MessageBox.Show("Location no longer exists.", "Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void ClearButton_Click(object? sender, EventArgs e)
    {
        string tabName = tabControl.SelectedIndex == 0 ? "singles" : "playlists";

        var result = MessageBox.Show(
            $"Are you sure you want to clear all {tabName} history?\n\nThis will not delete the downloaded files.",
            "Clear History",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            if (tabControl.SelectedIndex == 0)
                history.ClearSingles();
            else
                history.ClearPlaylists();

            CacheData(); // Reload from XML after clearing
            RefreshDisplay();
        }
    }
}
