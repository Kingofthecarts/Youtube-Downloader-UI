using System.Diagnostics;

namespace Youtube_Downloader;

public class SongBrowserForm : Form
{
    private DataGridView songsGrid = null!;
    private TextBox searchBox = null!;
    private ComboBox folderFilterComboBox = null!;
    private Label countLabel = null!;
    private CheckBox enableDeleteCheckBox = null!;
    private CheckBox showFolderCheckBox = null!;
    private CheckBox showDateCheckBox = null!;
    private CheckBox showLocateCheckBox = null!;
    private CheckBox showCommentsCheckBox = null!;
    private CheckBox showSizeCheckBox = null!;
    private DataGridViewButtonColumn? deleteColumn;
    private DataGridViewTextBoxColumn? folderColumn;
    private DataGridViewTextBoxColumn? dateColumn;
    private DataGridViewButtonColumn? locateColumn;
    private DataGridViewTextBoxColumn? commentsColumn;
    private DataGridViewTextBoxColumn? ratingColumn;
    private DataGridViewTextBoxColumn? sizeColumn;
    private ComboBox ratingFilterComboBox = null!;
    private readonly string outputFolder;
    private readonly Config config;
    private List<SongInfo> allSongs = new();
    private string currentSearchText = "";
    private string currentFolderFilter = "";
    private int currentRatingFilter = -1;  // -1 = all, 0 = unrated, 1-5 = specific rating
    private int currentPlayingRowIndex = -1;

    // Media player controls
    private MediaPlayerManager? mediaPlayer;
    private GroupBox playerGroupBox = null!;
    private Label playerTitleLabel = null!;
    private Label playerTimeLabel = null!;
    private TrackBar playerSeekBar = null!;
    private Button playerPlayPauseButton = null!;
    private Button playerStopButton = null!;
    private Button playerNextButton = null!;
    private Button playerRandomButton = null!;
    private TrackBar playerVolumeBar = null!;
    private CheckBox autoPlayNextCheckBox = null!;
    private SafePictureBox playerAlbumArt = null!;
    private Button resetViewButton = null!;
    private readonly Random random = new();

    public SongBrowserForm(string outputFolder, Config config, MainForm mainForm)
    {
        this.outputFolder = outputFolder;
        this.config = config;
        InitializeComponents();
        InitializeMediaPlayer();
        LoadSongs();
    }

    private void InitializeComponents()
    {
        Text = "Song Browser";
        Size = new Size(1050, 720);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 600);
        MaximizeBox = true;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Search section
        var searchLabel = new Label
        {
            Text = "Search:",
            Location = new Point(12, 15),
            Size = new Size(50, 23),
            TextAlign = ContentAlignment.MiddleLeft
        };

        searchBox = new TextBox
        {
            Location = new Point(65, 12),
            Size = new Size(250, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        var clearSearchButton = new Button
        {
            Text = "Clear",
            Location = new Point(325, 11),
            Size = new Size(60, 25)
        };
        clearSearchButton.Click += (s, e) => { searchBox.Clear(); };

        var folderFilterLabel = new Label
        {
            Text = "Folder:",
            Location = new Point(395, 15),
            Size = new Size(45, 23),
            TextAlign = ContentAlignment.MiddleLeft
        };

        folderFilterComboBox = new ComboBox
        {
            Location = new Point(440, 12),
            Size = new Size(120, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        folderFilterComboBox.SelectedIndexChanged += FolderFilterComboBox_SelectedIndexChanged;

        countLabel = new Label
        {
            Text = "0 songs",
            Location = new Point(570, 15),
            Size = new Size(100, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        // Show Date checkbox (off by default)
        showDateCheckBox = new CheckBox
        {
            Text = "Date",
            Location = new Point(675, 14),
            AutoSize = true,
            Checked = false
        };
        showDateCheckBox.CheckedChanged += ShowDateCheckBox_CheckedChanged;

        // Show Locate checkbox (off by default)
        showLocateCheckBox = new CheckBox
        {
            Text = "Locate",
            Location = new Point(735, 14),
            AutoSize = true,
            Checked = false
        };
        showLocateCheckBox.CheckedChanged += ShowLocateCheckBox_CheckedChanged;

        // Show Folder checkbox
        showFolderCheckBox = new CheckBox
        {
            Text = "Folder",
            Location = new Point(805, 14),
            AutoSize = true,
            Checked = false
        };
        showFolderCheckBox.CheckedChanged += ShowFolderCheckBox_CheckedChanged;

        // Show Comments checkbox (off by default)
        showCommentsCheckBox = new CheckBox
        {
            Text = "Comments",
            Location = new Point(870, 14),
            AutoSize = true,
            Checked = false
        };
        showCommentsCheckBox.CheckedChanged += ShowCommentsCheckBox_CheckedChanged;

        // Show Size checkbox (off by default)
        showSizeCheckBox = new CheckBox
        {
            Text = "Size",
            Location = new Point(960, 14),
            AutoSize = true,
            Checked = false
        };
        showSizeCheckBox.CheckedChanged += ShowSizeCheckBox_CheckedChanged;

        // Enable delete checkbox (only visible if AllowSongDelete is true in config)
        enableDeleteCheckBox = new CheckBox
        {
            Text = "Enable Delete",
            Location = new Point(12, 14),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Visible = config.AllowSongDelete
        };
        enableDeleteCheckBox.CheckedChanged += EnableDeleteCheckBox_CheckedChanged;

        // Songs grid
        songsGrid = new DataGridView
        {
            Location = new Point(12, 45),
            Size = new Size(1010, 380),
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
            // Selection colors (deeper blue when clicked/selected)
            DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(51, 153, 255),  // Deeper blue for selection
                SelectionForeColor = Color.White
            }
        };
        // Enable hover highlighting
        songsGrid.CellMouseEnter += SongsGrid_CellMouseEnter;
        songsGrid.CellMouseLeave += SongsGrid_CellMouseLeave;

        // Columns: Filename, Song Name, Album Artist, Album, Rating, Play, Edit, Size
        // Optional columns (via checkboxes): Date, Locate, Folder, Comments
        // Text columns use Fill mode, button columns use fixed width
        songsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Filename", HeaderText = "Filename", FillWeight = 20, MinimumWidth = 100 });
        songsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SongName", HeaderText = "Song Name", FillWeight = 18, MinimumWidth = 80 });
        songsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AlbumArtist", HeaderText = "Album Artist", FillWeight = 15, MinimumWidth = 80 });
        songsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Album", HeaderText = "Album", FillWeight = 15, MinimumWidth = 80 });

        // Rating column - clickable stars
        ratingColumn = new DataGridViewTextBoxColumn
        {
            Name = "Rating",
            HeaderText = "Rating",
            Width = 75,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 10),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        };
        songsGrid.Columns.Add(ratingColumn);

        songsGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Play", HeaderText = "", Text = "Play", UseColumnTextForButtonValue = true, Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        songsGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "Edit", UseColumnTextForButtonValue = true, Width = 45, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        // Size column is optional (added via checkbox)

        songsGrid.CellContentClick += SongsGrid_CellContentClick;
        songsGrid.CellDoubleClick += SongsGrid_CellDoubleClick;
        songsGrid.CellClick += SongsGrid_CellClick;  // For rating clicks

        // Media Player section (with album art)
        playerGroupBox = new GroupBox
        {
            Text = "Media Player",
            Location = new Point(12, 435),
            Size = new Size(1010, 90),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Album art thumbnail on the left
        playerAlbumArt = new SafePictureBox
        {
            Location = new Point(10, 18),
            Size = new Size(65, 65),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        playerTitleLabel = new Label
        {
            Text = "No track loaded",
            Location = new Point(80, 18),
            Size = new Size(330, 18),
            AutoEllipsis = true
        };

        playerTimeLabel = new Label
        {
            Text = "0:00 / 0:00",
            Location = new Point(420, 18),
            Size = new Size(90, 18),
            TextAlign = ContentAlignment.MiddleRight
        };

        playerPlayPauseButton = new Button
        {
            Text = "Play",
            Location = new Point(80, 40),
            Size = new Size(50, 23),
            Enabled = false
        };
        playerPlayPauseButton.Click += PlayerPlayPauseButton_Click;

        playerStopButton = new Button
        {
            Text = "Stop",
            Location = new Point(135, 40),
            Size = new Size(50, 23),
            Enabled = false
        };
        playerStopButton.Click += PlayerStopButton_Click;

        playerNextButton = new Button
        {
            Text = "Next",
            Location = new Point(190, 40),
            Size = new Size(50, 23),
            Enabled = false
        };
        playerNextButton.Click += PlayerNextButton_Click;

        playerRandomButton = new Button
        {
            Text = "Random",
            Location = new Point(245, 40),
            Size = new Size(60, 23)
        };
        playerRandomButton.Click += PlayerRandomButton_Click;

        playerSeekBar = new TrackBar
        {
            Location = new Point(310, 37),
            Size = new Size(380, 30),
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.BottomRight,
            TickFrequency = 100,  // Tick every 10%
            Enabled = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        playerSeekBar.Scroll += PlayerSeekBar_Scroll;
        playerSeekBar.MouseDown += PlayerSeekBar_MouseDown;

        var volumeLabel = new Label
        {
            Text = "Vol:",
            Location = new Point(700, 42),
            Size = new Size(28, 18),
            Anchor = AnchorStyles.Right
        };

        playerVolumeBar = new TrackBar
        {
            Location = new Point(728, 37),
            Size = new Size(100, 30),
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            TickStyle = TickStyle.BottomRight,
            TickFrequency = 25,  // Tick every 25%
            Anchor = AnchorStyles.Right
        };
        playerVolumeBar.Scroll += PlayerVolumeBar_Scroll;

        autoPlayNextCheckBox = new CheckBox
        {
            Text = "Auto-play next",
            Location = new Point(840, 40),
            AutoSize = true,
            Checked = false,
            Anchor = AnchorStyles.Right
        };

        playerGroupBox.Controls.Add(playerAlbumArt);
        playerGroupBox.Controls.Add(playerTitleLabel);
        playerGroupBox.Controls.Add(playerTimeLabel);
        playerGroupBox.Controls.Add(playerPlayPauseButton);
        playerGroupBox.Controls.Add(playerStopButton);
        playerGroupBox.Controls.Add(playerNextButton);
        playerGroupBox.Controls.Add(playerRandomButton);
        playerGroupBox.Controls.Add(playerSeekBar);
        playerGroupBox.Controls.Add(volumeLabel);
        playerGroupBox.Controls.Add(playerVolumeBar);
        playerGroupBox.Controls.Add(autoPlayNextCheckBox);

        // Bottom buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Location = new Point(12, 535),
            Size = new Size(1010, 35),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FlowDirection = FlowDirection.RightToLeft
        };

        var closeButton = new Button
        {
            Text = "Close",
            Size = new Size(80, 28)
        };
        closeButton.Click += (s, e) => Close();

        var openFolderButton = new Button
        {
            Text = "Open Output Folder",
            Size = new Size(120, 28)
        };
        openFolderButton.Click += (s, e) =>
        {
            if (Directory.Exists(outputFolder))
            {
                Process.Start("explorer.exe", outputFolder);
            }
        };

        var refreshButton = new Button
        {
            Text = "Refresh",
            Size = new Size(70, 28)
        };
        refreshButton.Click += (s, e) => { LoadSongs(); };

        // Rating filter (next to Refresh)
        ratingFilterComboBox = new ComboBox
        {
            Size = new Size(90, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(3, 3, 0, 3)
        };
        ratingFilterComboBox.Items.AddRange(new object[] { "(All)", "Unrated", "★", "★★", "★★★", "★★★★", "★★★★★" });
        ratingFilterComboBox.SelectedIndex = 0;
        ratingFilterComboBox.SelectedIndexChanged += RatingFilterComboBox_SelectedIndexChanged;

        var ratingLabel = new Label
        {
            Text = "Rating:",
            Size = new Size(45, 28),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(3, 5, 0, 3)
        };

        resetViewButton = new Button
        {
            Text = "Reset View",
            Size = new Size(80, 28)
        };
        resetViewButton.Click += ResetViewButton_Click;

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(openFolderButton);
        buttonPanel.Controls.Add(refreshButton);
        buttonPanel.Controls.Add(ratingFilterComboBox);
        buttonPanel.Controls.Add(ratingLabel);
        buttonPanel.Controls.Add(resetViewButton);

        Controls.Add(searchLabel);
        Controls.Add(searchBox);
        Controls.Add(clearSearchButton);
        Controls.Add(folderFilterLabel);
        Controls.Add(folderFilterComboBox);
        Controls.Add(countLabel);
        Controls.Add(showDateCheckBox);
        Controls.Add(showLocateCheckBox);
        Controls.Add(showFolderCheckBox);
        Controls.Add(showCommentsCheckBox);
        Controls.Add(showSizeCheckBox);
        Controls.Add(enableDeleteCheckBox);
        Controls.Add(songsGrid);
        Controls.Add(playerGroupBox);
        Controls.Add(buttonPanel);
    }

    private void ShowSizeCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (showSizeCheckBox.Checked && sizeColumn == null)
        {
            sizeColumn = new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "Size",
                Width = 60,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            songsGrid.Columns.Add(sizeColumn);
            FilterAndDisplaySongs();
        }
        else if (!showSizeCheckBox.Checked && sizeColumn != null)
        {
            songsGrid.Columns.Remove(sizeColumn);
            sizeColumn = null;
        }
    }

    private void ShowDateCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (showDateCheckBox.Checked)
        {
            if (dateColumn == null)
            {
                dateColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Date",
                    HeaderText = "Date",
                    FillWeight = 12,
                    MinimumWidth = 100
                };
                songsGrid.Columns.Add(dateColumn);
            }
            FilterAndDisplaySongs();
        }
        else
        {
            if (dateColumn != null && songsGrid.Columns.Contains(dateColumn))
            {
                songsGrid.Columns.Remove(dateColumn);
                dateColumn = null;
            }
        }
    }

    private void ShowLocateCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (showLocateCheckBox.Checked)
        {
            if (locateColumn == null)
            {
                locateColumn = new DataGridViewButtonColumn
                {
                    Name = "Locate",
                    HeaderText = "",
                    Text = "Locate",
                    UseColumnTextForButtonValue = true,
                    Width = 55,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                songsGrid.Columns.Add(locateColumn);
            }
        }
        else
        {
            if (locateColumn != null && songsGrid.Columns.Contains(locateColumn))
            {
                songsGrid.Columns.Remove(locateColumn);
                locateColumn = null;
            }
        }
    }

    private void ShowFolderCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (showFolderCheckBox.Checked)
        {
            if (folderColumn == null)
            {
                folderColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Folder",
                    HeaderText = "Folder",
                    FillWeight = 15,
                    MinimumWidth = 80
                };
                songsGrid.Columns.Add(folderColumn);
            }
            FilterAndDisplaySongs();
        }
        else
        {
            if (folderColumn != null && songsGrid.Columns.Contains(folderColumn))
            {
                songsGrid.Columns.Remove(folderColumn);
                folderColumn = null;
            }
        }
    }

    private void ShowCommentsCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (showCommentsCheckBox.Checked)
        {
            if (commentsColumn == null)
            {
                commentsColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Comments",
                    HeaderText = "Comments",
                    FillWeight = 15,
                    MinimumWidth = 80
                };
                songsGrid.Columns.Add(commentsColumn);
            }
            FilterAndDisplaySongs();
        }
        else
        {
            if (commentsColumn != null && songsGrid.Columns.Contains(commentsColumn))
            {
                songsGrid.Columns.Remove(commentsColumn);
                commentsColumn = null;
            }
        }
    }

    private void InitializeMediaPlayer()
    {
        // Use the shared media player instance so music continues when form closes
        mediaPlayer = SharedMediaPlayer.Instance;
        mediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
        mediaPlayer.PlaybackStopped += MediaPlayer_PlaybackStopped;
        mediaPlayer.TrackEnded += MediaPlayer_TrackEnded;
        SharedMediaPlayer.TrackStarted += SharedMediaPlayer_TrackStarted;

        // If already playing, update UI to reflect current state
        if (mediaPlayer.HasTrack)
        {
            UpdatePlayerUI();
            if (!string.IsNullOrEmpty(mediaPlayer.CurrentFile))
            {
                LoadAlbumArt(mediaPlayer.CurrentFile);
            }
        }
    }

    private void SharedMediaPlayer_TrackStarted(object? sender, TrackStartedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => SharedMediaPlayer_TrackStarted(sender, e));
            return;
        }

        // Update UI when track starts from another source (e.g., MainForm)
        UpdatePlayerUI();
        LoadAlbumArt(e.FilePath);
    }

    private void EnableDeleteCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (enableDeleteCheckBox.Checked)
        {
            // Add delete column
            if (deleteColumn == null)
            {
                deleteColumn = new DataGridViewButtonColumn
                {
                    Name = "Delete",
                    HeaderText = "",
                    Text = "X",
                    UseColumnTextForButtonValue = true,
                    Width = 30,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                };
                songsGrid.Columns.Add(deleteColumn);
            }
        }
        else
        {
            // Remove delete column
            if (deleteColumn != null && songsGrid.Columns.Contains(deleteColumn))
            {
                songsGrid.Columns.Remove(deleteColumn);
                deleteColumn = null;
            }
        }
    }

    private void LoadSongs()
    {
        var stopwatch = Stopwatch.StartNew();
        allSongs.Clear();

        if (!Directory.Exists(outputFolder))
        {
            countLabel.Text = "Output folder not found";
            return;
        }

        try
        {
            // Get all MP3 files recursively
            var mp3Files = Directory.GetFiles(outputFolder, "*.mp3", SearchOption.AllDirectories);

            foreach (var filePath in mp3Files)
            {
                var fileInfo = new FileInfo(filePath);
                string relativePath = Path.GetDirectoryName(filePath) ?? "";

                // Get folder relative to output folder
                string folder = "";
                if (relativePath.StartsWith(outputFolder, StringComparison.OrdinalIgnoreCase))
                {
                    folder = relativePath.Substring(outputFolder.Length).TrimStart(Path.DirectorySeparatorChar);
                }

                var songInfo = new SongInfo
                {
                    Filename = Path.GetFileNameWithoutExtension(filePath),
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    Folder = folder,
                    DateCreated = fileInfo.CreationTime,
                    FileSizeBytes = fileInfo.Length
                };

                // Try to read ID3 tags
                try
                {
                    using var tagFile = TagLib.File.Create(filePath);
                    songInfo.SongName = tagFile.Tag.Title ?? "";
                    songInfo.AlbumArtist = tagFile.Tag.FirstAlbumArtist ?? "";
                    songInfo.Album = tagFile.Tag.Album ?? "";
                    songInfo.Comments = tagFile.Tag.Comment ?? "";

                    // Read rating from ID3v2 POPM frame
                    if (tagFile.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
                    {
                        var id3v2Tag = (TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2);
                        var popmFrames = id3v2Tag.GetFrames<TagLib.Id3v2.PopularimeterFrame>();
                        if (popmFrames.Any())
                        {
                            songInfo.Rating = SongInfo.ByteToStars(popmFrames.First().Rating);
                        }
                    }
                }
                catch
                {
                    // If we can't read tags, leave them empty
                }

                allSongs.Add(songInfo);
            }

            // Sort by date descending (newest first)
            allSongs = allSongs.OrderByDescending(s => s.DateCreated).ToList();
        }
        catch (Exception ex)
        {
            countLabel.Text = $"Error: {ex.Message}";
            return;
        }

        // Populate folder filter dropdown
        PopulateFolderFilter();

        FilterAndDisplaySongs();

        stopwatch.Stop();
        if (config.EnablePerformanceTracking)
        {
            config.LastSongLoadMs = (int)stopwatch.ElapsedMilliseconds;
            config.Save();
        }
    }

    private bool isPopulatingFolderFilter = false;

    private void PopulateFolderFilter()
    {
        isPopulatingFolderFilter = true;
        try
        {
            string previousSelection = currentFolderFilter;

            folderFilterComboBox.Items.Clear();
            folderFilterComboBox.Items.Add("(All)");

            // Get unique folder names, sorted alphabetically
            var folders = allSongs
                .Select(s => string.IsNullOrEmpty(s.Folder) ? "(Root)" : s.Folder)
                .Distinct()
                .OrderBy(f => f == "(Root)" ? "" : f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var folder in folders)
            {
                folderFilterComboBox.Items.Add(folder);
            }

            // Restore previous selection if still valid
            if (!string.IsNullOrEmpty(previousSelection) && folderFilterComboBox.Items.Contains(previousSelection))
            {
                folderFilterComboBox.SelectedItem = previousSelection;
            }
            else
            {
                folderFilterComboBox.SelectedIndex = 0; // Select "(All)"
                currentFolderFilter = "";
            }
        }
        finally
        {
            isPopulatingFolderFilter = false;
        }
    }

    private void FolderFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isPopulatingFolderFilter) return;
        if (folderFilterComboBox.SelectedItem == null) return;

        string selected = folderFilterComboBox.SelectedItem.ToString() ?? "";

        if (selected == "(All)")
        {
            currentFolderFilter = "";
        }
        else if (selected == "(Root)")
        {
            currentFolderFilter = "(Root)";
        }
        else
        {
            currentFolderFilter = selected;
        }

        FilterAndDisplaySongs();
    }

    private void FilterAndDisplaySongs()
    {
        songsGrid.Rows.Clear();
        currentPlayingRowIndex = -1;

        // Start with all songs
        IEnumerable<SongInfo> filteredSongs = allSongs;

        // Apply folder filter
        if (!string.IsNullOrEmpty(currentFolderFilter))
        {
            if (currentFolderFilter == "(Root)")
            {
                filteredSongs = filteredSongs.Where(s => string.IsNullOrEmpty(s.Folder));
            }
            else
            {
                filteredSongs = filteredSongs.Where(s =>
                    s.Folder.Equals(currentFolderFilter, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Apply rating filter
        if (currentRatingFilter >= 0)
        {
            filteredSongs = filteredSongs.Where(s => s.Rating == currentRatingFilter);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            filteredSongs = filteredSongs.Where(s => MatchesSearch(s));
        }

        var filteredList = filteredSongs.ToList();

        foreach (var song in filteredList)
        {
            int rowIndex = songsGrid.Rows.Add();
            var row = songsGrid.Rows[rowIndex];

            // Set base column values by name
            row.Cells["Filename"].Value = song.Filename;
            row.Cells["SongName"].Value = song.SongName;
            row.Cells["AlbumArtist"].Value = song.AlbumArtist;
            row.Cells["Album"].Value = song.Album;
            row.Cells["Rating"].Value = song.RatingStars;
            row.Cells["Play"].Value = "Play";
            row.Cells["Edit"].Value = "Edit";

            // Set optional column values if they exist
            if (sizeColumn != null)
            {
                row.Cells["Size"].Value = song.FileSizeFormatted;
            }
            if (dateColumn != null)
            {
                row.Cells["Date"].Value = song.DateCreated.ToString("yyyy-MM-dd HH:mm");
            }
            if (locateColumn != null)
            {
                row.Cells["Locate"].Value = "Locate";
            }
            if (folderColumn != null)
            {
                row.Cells["Folder"].Value = song.Folder;
            }
            if (commentsColumn != null)
            {
                row.Cells["Comments"].Value = song.Comments;
            }

            row.Tag = song;

            // Track if this is the currently playing song
            if (mediaPlayer?.CurrentFile == song.FilePath)
            {
                currentPlayingRowIndex = rowIndex;
                // Highlight the playing row
                songsGrid.Rows[rowIndex].DefaultCellStyle.BackColor = PlayingRowColor;
            }

            // Highlight matching cells
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                HighlightMatchingCells(songsGrid.Rows[rowIndex], song);
            }
        }

        int total = allSongs.Count;
        int shown = filteredList.Count;
        bool isFiltered = !string.IsNullOrEmpty(currentSearchText) || !string.IsNullOrEmpty(currentFolderFilter) || currentRatingFilter >= 0;
        countLabel.Text = isFiltered
            ? $"{shown} of {total} songs"
            : $"{total} songs";
    }

    private bool MatchesSearch(SongInfo song)
    {
        return song.Filename.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               song.SongName.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               song.AlbumArtist.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               song.Album.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               song.Folder.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
               song.Comments.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void HighlightMatchingCells(DataGridViewRow row, SongInfo song)
    {
        if (song.Filename.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Filename"].Style.BackColor = Color.Yellow;

        if (song.SongName.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["SongName"].Style.BackColor = Color.Yellow;

        if (song.AlbumArtist.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["AlbumArtist"].Style.BackColor = Color.Yellow;

        if (song.Album.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Album"].Style.BackColor = Color.Yellow;

        if (dateColumn != null && song.DateCreated.ToString("yyyy-MM-dd HH:mm").Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Date"].Style.BackColor = Color.Yellow;

        if (folderColumn != null && song.Folder.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Folder"].Style.BackColor = Color.Yellow;

        if (commentsColumn != null && song.Comments.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            row.Cells["Comments"].Style.BackColor = Color.Yellow;
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        currentSearchText = searchBox.Text.Trim();
        FilterAndDisplaySongs();
    }

    private void RatingFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        int selectedIndex = ratingFilterComboBox.SelectedIndex;
        // Index 0 = (All), 1 = Unrated (0 stars), 2-6 = 1-5 stars
        currentRatingFilter = selectedIndex switch
        {
            0 => -1,  // All
            1 => 0,   // Unrated
            _ => selectedIndex - 1  // 1-5 stars
        };
        FilterAndDisplaySongs();
    }

    private void SongsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        string columnName = songsGrid.Columns[e.ColumnIndex].Name;
        if (columnName != "Rating") return;

        var song = songsGrid.Rows[e.RowIndex].Tag as SongInfo;
        if (song == null) return;

        // Calculate which star was clicked based on mouse position
        var cell = songsGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
        var mousePos = songsGrid.PointToClient(Cursor.Position);
        int relativeX = mousePos.X - cell.X;

        // Each star is approximately cell.Width / 5 pixels wide
        int starWidth = cell.Width / 5;
        int clickedStar = Math.Min(5, Math.Max(1, (relativeX / starWidth) + 1));

        // Toggle: if clicking the same star that's the current rating, clear it
        int newRating = (song.Rating == clickedStar) ? 0 : clickedStar;

        // Save rating to file
        if (SaveRatingToFile(song.FilePath, newRating))
        {
            song.Rating = newRating;
            songsGrid.Rows[e.RowIndex].Cells["Rating"].Value = song.RatingStars;
        }
    }

    private bool SaveRatingToFile(string filePath, int rating)
    {
        try
        {
            using var tagFile = TagLib.File.Create(filePath);

            // Get or create ID3v2 tag
            var id3v2Tag = (TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, true);

            // Remove existing POPM frames
            id3v2Tag.RemoveFrames("POPM");

            if (rating > 0)
            {
                // Add new POPM frame with rating
                var popmFrame = new TagLib.Id3v2.PopularimeterFrame("Windows Media Player 9 Series")
                {
                    Rating = SongInfo.StarsToByte(rating),
                    PlayCount = 0
                };
                id3v2Tag.AddFrame(popmFrame);
            }

            tagFile.Save();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save rating: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void SongsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        var song = songsGrid.Rows[e.RowIndex].Tag as SongInfo;
        if (song == null) return;

        string columnName = songsGrid.Columns[e.ColumnIndex].Name;

        if (columnName == "Play")
        {
            PlaySong(song, e.RowIndex);
        }
        else if (columnName == "Edit")
        {
            EditSong(song, e.RowIndex);
        }
        else if (columnName == "Locate")
        {
            LocateSong(song);
        }
        else if (columnName == "Delete")
        {
            DeleteSong(song);
        }
    }

    private void SongsGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        // Double-click no longer plays - only the Play button should play
        // This handler is kept in case we want to add other double-click behavior later
    }

    private void PlaySong(SongInfo song, int rowIndex = -1)
    {
        if (!File.Exists(song.FilePath))
        {
            MessageBox.Show("File not found. It may have been moved or deleted.",
                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Reset previous playing row's highlight
        if (currentPlayingRowIndex >= 0 && currentPlayingRowIndex < songsGrid.Rows.Count)
        {
            songsGrid.Rows[currentPlayingRowIndex].DefaultCellStyle.BackColor = Color.Empty;
        }

        currentPlayingRowIndex = rowIndex;
        // Use SharedMediaPlayer to notify all listeners (MainForm player will also update)
        SharedMediaPlayer.Play(song.FilePath, song.Filename);
        LoadAlbumArt(song.FilePath);

        // Highlight the entire row light green when playing
        if (rowIndex >= 0 && rowIndex < songsGrid.Rows.Count)
        {
            songsGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(198, 239, 206);  // Light green row
        }

        UpdatePlayerUI();
    }

    /// <summary>
    /// Play a file by its full path. Called from external sources like HistoryForm.
    /// </summary>
    public void PlayFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                $"Song not found. The file may have been deleted or renamed.\n\nExpected path: {filePath}",
                "Song Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Find the song in the grid if it exists
        int rowIndex = -1;
        for (int i = 0; i < songsGrid.Rows.Count; i++)
        {
            var song = songsGrid.Rows[i].Tag as SongInfo;
            if (song != null && song.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                rowIndex = i;
                songsGrid.ClearSelection();
                songsGrid.Rows[i].Selected = true;
                songsGrid.FirstDisplayedScrollingRowIndex = i;
                break;
            }
        }

        // Play the file directly
        currentPlayingRowIndex = rowIndex;
        string filename = Path.GetFileNameWithoutExtension(filePath);
        SharedMediaPlayer.Play(filePath, filename);
        LoadAlbumArt(filePath);
        UpdatePlayerUI();
    }

    /// <summary>
    /// Find and play a song by video ID stored in comments. Returns true if found.
    /// </summary>
    public bool PlayByVideoId(string videoId)
    {
        if (string.IsNullOrEmpty(videoId)) return false;

        // Search all songs for matching video ID in comments
        var matchingSong = allSongs.FirstOrDefault(s =>
            s.Comments.Equals(videoId, StringComparison.OrdinalIgnoreCase));

        if (matchingSong != null && File.Exists(matchingSong.FilePath))
        {
            // Find and select in grid
            int rowIndex = -1;
            for (int i = 0; i < songsGrid.Rows.Count; i++)
            {
                var song = songsGrid.Rows[i].Tag as SongInfo;
                if (song != null && song.FilePath.Equals(matchingSong.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    rowIndex = i;
                    songsGrid.ClearSelection();
                    songsGrid.Rows[i].Selected = true;
                    songsGrid.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }

            currentPlayingRowIndex = rowIndex;
            SharedMediaPlayer.Play(matchingSong.FilePath, matchingSong.Filename);
            LoadAlbumArt(matchingSong.FilePath);
            UpdatePlayerUI();
            return true;
        }

        return false;
    }

    private void LoadAlbumArt(string filePath)
    {
        // Clear previous image
        playerAlbumArt.Image?.Dispose();
        playerAlbumArt.Image = null;

        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            if (tagFile.Tag.Pictures.Length > 0)
            {
                var picture = tagFile.Tag.Pictures[0];
                // Create a copy so we don't depend on the stream staying open
                using var ms = new MemoryStream(picture.Data.Data);
                using var tempImage = Image.FromStream(ms);
                playerAlbumArt.Image?.Dispose();
                playerAlbumArt.Image = new Bitmap(tempImage);
            }
        }
        catch
        {
            // If we can't read the album art, leave it blank
        }
    }

    private void EditSong(SongInfo song, int rowIndex)
    {
        if (!File.Exists(song.FilePath))
        {
            MessageBox.Show("File not found. It may have been moved or deleted.",
                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadSongs();
            return;
        }

        // Check if this song is currently playing - save position
        bool wasPlaying = mediaPlayer?.IsPlaying == true && mediaPlayer?.CurrentFile == song.FilePath;
        bool wasPaused = mediaPlayer?.IsPaused == true && mediaPlayer?.CurrentFile == song.FilePath;
        TimeSpan savedPosition = TimeSpan.Zero;
        string? oldFilePath = song.FilePath;

        if (wasPlaying || wasPaused)
        {
            savedPosition = mediaPlayer!.Position;
            mediaPlayer.Stop();
        }

        using var renameForm = new RenameForm(song.FilePath, null, config.AllowFilenameEdit);
        if (renameForm.ShowDialog(this) == DialogResult.OK && renameForm.ChangesMade)
        {
            // Update song info with new path/name
            song.FilePath = renameForm.NewFilePath;
            song.Filename = Path.GetFileNameWithoutExtension(renameForm.NewFilePath);
            song.FileName = Path.GetFileName(renameForm.NewFilePath);

            // Re-read ID3 tags
            try
            {
                using var tagFile = TagLib.File.Create(song.FilePath);
                song.SongName = tagFile.Tag.Title ?? "";
                song.AlbumArtist = tagFile.Tag.FirstAlbumArtist ?? "";
                song.Album = tagFile.Tag.Album ?? "";
            }
            catch
            {
                // If we can't read tags, leave them as is
            }

            // Update grid display
            songsGrid.Rows[rowIndex].Cells["Filename"].Value = song.Filename;
            songsGrid.Rows[rowIndex].Cells["SongName"].Value = song.SongName;
            songsGrid.Rows[rowIndex].Cells["AlbumArtist"].Value = song.AlbumArtist;
            songsGrid.Rows[rowIndex].Cells["Album"].Value = song.Album;

            // Resume playback at same position if was playing
            if (wasPlaying || wasPaused)
            {
                // Load file at saved position without playing yet (no audio blip)
                mediaPlayer!.LoadPaused(song.FilePath, savedPosition, song.Filename);
                if (wasPlaying)
                {
                    // Resume playback from the saved position
                    mediaPlayer.Resume();
                }
                // If wasPaused, it's already paused from LoadPaused
                LoadAlbumArt(song.FilePath);
                UpdatePlayerUI();
            }
        }
        else if (wasPlaying || wasPaused)
        {
            // If rename was cancelled or no changes made, resume playback at same position (no audio blip)
            mediaPlayer!.LoadPaused(oldFilePath!, savedPosition, song.Filename);
            if (wasPlaying)
            {
                mediaPlayer.Resume();
            }
            LoadAlbumArt(oldFilePath!);
            UpdatePlayerUI();
        }
    }

    private void LocateSong(SongInfo song)
    {
        if (!File.Exists(song.FilePath))
        {
            MessageBox.Show("File not found. It may have been moved or deleted.",
                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start("explorer.exe", $"/select,\"{song.FilePath}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to locate file: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteSong(SongInfo song)
    {
        if (!File.Exists(song.FilePath))
        {
            MessageBox.Show("File not found. It may have been moved or deleted.",
                "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadSongs(); // Refresh list
            return;
        }

        // Stop playback if this song is playing
        if (mediaPlayer?.CurrentFile == song.FilePath)
        {
            mediaPlayer.Stop();
            UpdatePlayerUI();
        }

        // Show confirmation dialog with countdown
        using var confirmForm = new DeleteConfirmForm(song.Filename, config.SongDeleteRequireCountdown, config.SongDeleteCountdownSeconds);
        if (confirmForm.ShowDialog(this) == DialogResult.Yes)
        {
            try
            {
                File.Delete(song.FilePath);
                allSongs.Remove(song);
                FilterAndDisplaySongs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete file: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Media Player event handlers
    private void PlayerPlayPauseButton_Click(object? sender, EventArgs e)
    {
        if (mediaPlayer == null) return;

        if (mediaPlayer.IsPlaying)
        {
            mediaPlayer.Pause();
            playerPlayPauseButton.Text = "Play";
        }
        else if (mediaPlayer.IsPaused)
        {
            mediaPlayer.Resume();
            playerPlayPauseButton.Text = "Pause";
        }
    }

    private void PlayerStopButton_Click(object? sender, EventArgs e)
    {
        // Reset the playing row's highlight before stopping
        if (currentPlayingRowIndex >= 0 && currentPlayingRowIndex < songsGrid.Rows.Count)
        {
            songsGrid.Rows[currentPlayingRowIndex].DefaultCellStyle.BackColor = Color.Empty;
        }
        currentPlayingRowIndex = -1;

        SharedMediaPlayer.Stop();
        UpdatePlayerUI();
    }

    private void PlayerSeekBar_Scroll(object? sender, EventArgs e)
    {
        if (mediaPlayer != null && mediaPlayer.HasTrack)
        {
            double percent = playerSeekBar.Value / 1000.0;
            mediaPlayer.SeekPercent(percent);
        }
    }

    private void PlayerSeekBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (mediaPlayer == null || !mediaPlayer.HasTrack) return;

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
        mediaPlayer.SeekPercent(playerSeekBar.Value / 1000.0);
    }

    private void PlayerVolumeBar_Scroll(object? sender, EventArgs e)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Volume = playerVolumeBar.Value / 100f;
        }
    }

    private void MediaPlayer_PositionChanged(object? sender, TimeSpan position)
    {
        if (InvokeRequired)
        {
            Invoke(() => MediaPlayer_PositionChanged(sender, position));
            return;
        }

        if (mediaPlayer == null) return;

        var duration = mediaPlayer.Duration;
        playerTimeLabel.Text = $"{FormatTimeSpan(position)} / {FormatTimeSpan(duration)}";

        if (duration.TotalSeconds > 0)
        {
            int seekPosition = (int)(position.TotalSeconds / duration.TotalSeconds * 1000);
            playerSeekBar.Value = Math.Min(seekPosition, 1000);
        }
    }

    private void MediaPlayer_PlaybackStopped(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MediaPlayer_PlaybackStopped(sender, e));
            return;
        }

        // Reset the playing row's button color
        if (currentPlayingRowIndex >= 0 && currentPlayingRowIndex < songsGrid.Rows.Count)
        {
            var playCell = songsGrid.Rows[currentPlayingRowIndex].Cells["Play"];
            playCell.Style.BackColor = Color.Empty;
        }
        currentPlayingRowIndex = -1;

        UpdatePlayerUI();
    }

    private void MediaPlayer_TrackEnded(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(() => MediaPlayer_TrackEnded(sender, e));
            return;
        }

        // Auto-play next song if enabled (do this before stopping)
        if (autoPlayNextCheckBox.Checked && currentPlayingRowIndex >= 0)
        {
            int nextRowIndex = currentPlayingRowIndex + 1;
            if (nextRowIndex < songsGrid.Rows.Count)
            {
                var nextSong = songsGrid.Rows[nextRowIndex].Tag as SongInfo;
                if (nextSong != null)
                {
                    PlaySong(nextSong, nextRowIndex);
                    return;  // Don't stop, we're playing the next song
                }
            }
        }

        SharedMediaPlayer.Stop();
        UpdatePlayerUI();
    }

    private void UpdatePlayerUI()
    {
        if (mediaPlayer == null) return;

        bool hasTrack = mediaPlayer.HasTrack;
        playerPlayPauseButton.Enabled = hasTrack;
        playerStopButton.Enabled = hasTrack;
        playerSeekBar.Enabled = hasTrack;
        playerNextButton.Enabled = hasTrack && currentPlayingRowIndex >= 0 && currentPlayingRowIndex < songsGrid.Rows.Count - 1;

        if (hasTrack)
        {
            playerTitleLabel.Text = mediaPlayer.CurrentTitle ?? "Unknown";
            playerPlayPauseButton.Text = mediaPlayer.IsPlaying ? "Pause" : "Play";
        }
        else
        {
            playerTitleLabel.Text = "No track loaded";
            playerTimeLabel.Text = "0:00 / 0:00";
            playerSeekBar.Value = 0;
            playerPlayPauseButton.Text = "Play";
            // Clear album art when no track is loaded
            playerAlbumArt.Image?.Dispose();
            playerAlbumArt.Image = null;
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private int hoveredRowIndex = -1;
    private static readonly Color HoverColor = Color.FromArgb(204, 229, 255);  // Light blue for hover
    private static readonly Color PlayingRowColor = Color.FromArgb(198, 239, 206);  // Light green for playing row

    private void SongsGrid_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex != songsGrid.CurrentRow?.Index)
        {
            // Don't change hover color for currently playing row
            if (e.RowIndex == currentPlayingRowIndex) return;

            hoveredRowIndex = e.RowIndex;
            songsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = HoverColor;
        }
    }

    private void SongsGrid_CellMouseLeave(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && hoveredRowIndex == e.RowIndex)
        {
            hoveredRowIndex = -1;
            // Restore playing row color if this is the playing row, otherwise clear
            if (e.RowIndex == currentPlayingRowIndex)
            {
                songsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = PlayingRowColor;
            }
            else
            {
                songsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Empty;
            }
        }
    }

    private void PlayerNextButton_Click(object? sender, EventArgs e)
    {
        if (currentPlayingRowIndex >= 0 && currentPlayingRowIndex < songsGrid.Rows.Count - 1)
        {
            int nextRowIndex = currentPlayingRowIndex + 1;
            var nextSong = songsGrid.Rows[nextRowIndex].Tag as SongInfo;
            if (nextSong != null)
            {
                PlaySong(nextSong, nextRowIndex);
            }
        }
    }

    private void PlayerRandomButton_Click(object? sender, EventArgs e)
    {
        if (songsGrid.Rows.Count == 0) return;

        int randomIndex = random.Next(songsGrid.Rows.Count);
        var randomSong = songsGrid.Rows[randomIndex].Tag as SongInfo;
        if (randomSong != null)
        {
            PlaySong(randomSong, randomIndex);
            // Scroll to and select the random song
            songsGrid.ClearSelection();
            songsGrid.Rows[randomIndex].Selected = true;
            songsGrid.FirstDisplayedScrollingRowIndex = randomIndex;
        }
    }

    private void ResetViewButton_Click(object? sender, EventArgs e)
    {
        // Clear search
        searchBox.Text = "";
        currentSearchText = "";

        // Reset folder filter
        folderFilterComboBox.SelectedIndex = 0;
        currentFolderFilter = "";

        // Reset rating filter
        ratingFilterComboBox.SelectedIndex = 0;
        currentRatingFilter = -1;

        // Reset optional column checkboxes to off
        showDateCheckBox.Checked = false;
        showLocateCheckBox.Checked = false;
        showFolderCheckBox.Checked = false;
        showCommentsCheckBox.Checked = false;
        showSizeCheckBox.Checked = false;

        // Reset delete checkbox if visible
        if (enableDeleteCheckBox.Visible)
        {
            enableDeleteCheckBox.Checked = false;
        }

        // Refresh display (don't stop music)
        FilterAndDisplaySongs();

        // Clear selection
        songsGrid.ClearSelection();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Unsubscribe from events but DON'T dispose - the shared player keeps playing
        if (mediaPlayer != null)
        {
            mediaPlayer.PositionChanged -= MediaPlayer_PositionChanged;
            mediaPlayer.PlaybackStopped -= MediaPlayer_PlaybackStopped;
            mediaPlayer.TrackEnded -= MediaPlayer_TrackEnded;
        }
        SharedMediaPlayer.TrackStarted -= SharedMediaPlayer_TrackStarted;

        base.OnFormClosing(e);
    }
}

public class DeleteConfirmForm : Form
{
    private Button yesButton = null!;
    private Button noButton = null!;
    private Label messageLabel = null!;
    private Label countdownLabel = null!;
    private System.Windows.Forms.Timer? countdownTimer;
    private int remainingSeconds;
    private readonly bool requireCountdown;

    public DeleteConfirmForm(string songTitle, bool requireCountdown, int countdownSeconds)
    {
        this.requireCountdown = requireCountdown;
        this.remainingSeconds = countdownSeconds;

        Text = "Confirm Delete";
        Size = new Size(400, 180);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        messageLabel = new Label
        {
            Text = $"Are you sure you want to permanently delete:\n\n\"{songTitle}\"?",
            Location = new Point(20, 20),
            Size = new Size(350, 50),
            TextAlign = ContentAlignment.TopLeft
        };

        countdownLabel = new Label
        {
            Text = requireCountdown ? $"Please wait {remainingSeconds} seconds..." : "",
            Location = new Point(20, 75),
            Size = new Size(350, 20),
            ForeColor = Color.Red,
            Visible = requireCountdown
        };

        yesButton = new Button
        {
            Text = requireCountdown ? $"Yes ({remainingSeconds})" : "Yes",
            Location = new Point(200, 105),
            Size = new Size(80, 28),
            Enabled = !requireCountdown,
            DialogResult = DialogResult.Yes
        };

        noButton = new Button
        {
            Text = "No",
            Location = new Point(290, 105),
            Size = new Size(80, 28),
            DialogResult = DialogResult.No
        };

        Controls.Add(messageLabel);
        Controls.Add(countdownLabel);
        Controls.Add(yesButton);
        Controls.Add(noButton);

        AcceptButton = noButton; // Default to No
        CancelButton = noButton;

        if (requireCountdown && remainingSeconds > 0)
        {
            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }
    }

    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        remainingSeconds--;

        if (remainingSeconds <= 0)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            yesButton.Text = "Yes";
            yesButton.Enabled = true;
            countdownLabel.Text = "You may now delete the file.";
            countdownLabel.ForeColor = Color.Green;
        }
        else
        {
            yesButton.Text = $"Yes ({remainingSeconds})";
            countdownLabel.Text = $"Please wait {remainingSeconds} seconds...";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        countdownTimer?.Stop();
        countdownTimer?.Dispose();
        base.OnFormClosing(e);
    }
}

public class SongInfo
{
    public string Filename { get; set; } = "";  // Filename without extension
    public string FileName { get; set; } = "";  // Full filename with extension
    public string FilePath { get; set; } = "";
    public string Folder { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public long FileSizeBytes { get; set; }

    // ID3 tag fields
    public string SongName { get; set; } = "";  // Title tag
    public string AlbumArtist { get; set; } = "";
    public string Album { get; set; } = "";
    public string Comments { get; set; } = "";  // Comments tag (often contains video ID)
    public int Rating { get; set; } = 0;  // 0-5 stars (0 = unrated)

    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Returns star display string (e.g., "★★★☆☆" for 3 stars)
    /// </summary>
    public string RatingStars
    {
        get
        {
            if (Rating == 0) return "☆☆☆☆☆";
            return new string('★', Rating) + new string('☆', 5 - Rating);
        }
    }

    /// <summary>
    /// Converts ID3 rating byte (0-255) to 0-5 stars
    /// </summary>
    public static int ByteToStars(byte rating)
    {
        if (rating == 0) return 0;
        if (rating <= 31) return 1;   // 1-31
        if (rating <= 95) return 2;   // 32-95
        if (rating <= 159) return 3;  // 96-159
        if (rating <= 223) return 4;  // 160-223
        return 5;                      // 224-255
    }

    /// <summary>
    /// Converts 0-5 stars to ID3 rating byte (0-255)
    /// </summary>
    public static byte StarsToByte(int stars)
    {
        return stars switch
        {
            0 => 0,
            1 => 1,
            2 => 64,
            3 => 128,
            4 => 196,
            5 => 255,
            _ => 0
        };
    }
}
