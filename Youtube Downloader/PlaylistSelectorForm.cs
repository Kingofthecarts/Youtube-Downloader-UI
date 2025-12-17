using System.Diagnostics;
using System.Text.Json;

namespace Youtube_Downloader;

public class PlaylistItem
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public bool Selected { get; set; } = true;
    public int Index { get; set; }
}

public class PlaylistSelectorForm : Form
{
    private DataGridView dataGridView = null!;
    private Button selectAllButton = null!;
    private Button selectNoneButton = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;
    private Label statusLabel = null!;
    private ProgressBar loadingBar = null!;
    private TextBox searchBox = null!;
    private Button clearSearchButton = null!;
    private Label countLabel = null!;
    private Label searchLabel = null!;

    private readonly string ytDlpPath;
    private readonly string playlistUrl;
    private readonly Config config;
    private List<PlaylistItem> items = new();
    private string currentSearchText = "";

    public List<PlaylistItem> SelectedItems => items.Where(i => i.Selected).ToList();
    public List<PlaylistItem> ExcludedItems => items.Where(i => !i.Selected).ToList();
    public bool DialogConfirmed { get; private set; } = false;
    public string PlaylistTitle { get; private set; } = "";

    public PlaylistSelectorForm(string ytDlpPath, string playlistUrl, Config config)
    {
        this.ytDlpPath = ytDlpPath;
        this.playlistUrl = playlistUrl;
        this.config = config;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Select Playlist Songs";
        Size = new Size(700, 500);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(500, 350);

        // Status and loading
        statusLabel = new Label
        {
            Text = "Loading playlist...",
            Location = new Point(12, 12),
            AutoSize = true
        };

        loadingBar = new ProgressBar
        {
            Location = new Point(12, 35),
            Size = new Size(660, 20),
            Style = ProgressBarStyle.Marquee,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Search controls
        searchLabel = new Label
        {
            Text = "Search:",
            Location = new Point(12, 62),
            Size = new Size(50, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false
        };

        searchBox = new TextBox
        {
            Location = new Point(65, 60),
            Size = new Size(250, 23),
            Visible = false
        };
        searchBox.TextChanged += SearchBox_TextChanged;

        clearSearchButton = new Button
        {
            Text = "Clear",
            Location = new Point(325, 59),
            Size = new Size(60, 25),
            Visible = false
        };
        clearSearchButton.Click += (s, e) => { searchBox.Clear(); };

        countLabel = new Label
        {
            Text = "",
            Location = new Point(395, 62),
            Size = new Size(200, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false
        };

        Controls.Add(searchLabel);
        Controls.Add(searchBox);
        Controls.Add(clearSearchButton);
        Controls.Add(countLabel);

        // DataGridView
        dataGridView = new DataGridView
        {
            Location = new Point(12, 90),
            Size = new Size(660, 315),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            Visible = false
        };

        // Checkbox column
        var checkColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Selected",
            HeaderText = "",
            Width = 30
        };
        dataGridView.Columns.Add(checkColumn);

        // Index column
        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Index",
            HeaderText = "#",
            Width = 40,
            ReadOnly = true
        });

        // Title column
        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Title",
            HeaderText = "Title",
            Width = 520,
            ReadOnly = true
        });

        dataGridView.RowTemplate.Height = 25;
        dataGridView.CellContentClick += DataGridView_CellContentClick;

        // Buttons
        selectAllButton = new Button
        {
            Text = "Select All",
            Location = new Point(12, 415),
            Size = new Size(80, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Visible = false
        };
        selectAllButton.Click += SelectAllButton_Click;

        selectNoneButton = new Button
        {
            Text = "Select None",
            Location = new Point(100, 415),
            Size = new Size(85, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Visible = false
        };
        selectNoneButton.Click += SelectNoneButton_Click;

        saveButton = new Button
        {
            Text = "Download Selected",
            Location = new Point(480, 415),
            Size = new Size(110, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Visible = false
        };
        saveButton.Click += SaveButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(598, 415),
            Size = new Size(75, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        cancelButton.Click += (s, e) => { DialogConfirmed = false; Close(); };

        Controls.Add(statusLabel);
        Controls.Add(loadingBar);
        Controls.Add(dataGridView);
        Controls.Add(selectAllButton);
        Controls.Add(selectNoneButton);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        Load += PlaylistSelectorForm_Load;
    }

    private async void PlaylistSelectorForm_Load(object? sender, EventArgs e)
    {
        await LoadPlaylistAsync();
    }

    private string GetCookiesArgument()
    {
        var cookiePath = YouTubeCookieManager.GetCookiesFilePath(config);
        return !string.IsNullOrEmpty(cookiePath) ? $"--cookies \"{cookiePath}\" " : "";
    }

    private async Task LoadPlaylistAsync()
    {
        try
        {
            string cookiesArg = GetCookiesArgument();
            // Use yt-dlp to get playlist info as JSON
            var processInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"{cookiesArg}--flat-playlist -J \"{playlistUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception("Failed to load playlist information");
            }

            // Parse JSON
            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            // Get playlist title from YouTube
            if (root.TryGetProperty("title", out var playlistTitleElement))
            {
                PlaylistTitle = playlistTitleElement.GetString() ?? "";
            }

            if (root.TryGetProperty("entries", out var entries))
            {
                int index = 1;
                foreach (var entry in entries.EnumerateArray())
                {
                    var item = new PlaylistItem
                    {
                        Index = index++,
                        VideoId = entry.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        Title = entry.TryGetProperty("title", out var title) ? title.GetString() ?? "Unknown" : "Unknown",
                        Selected = true
                    };

                    item.Url = $"https://www.youtube.com/watch?v={item.VideoId}";

                    items.Add(item);
                }
            }

            // Populate grid
            PopulateGrid();

            statusLabel.Text = $"Found {items.Count} songs. Select which ones to download:";
            loadingBar.Visible = false;
            dataGridView.Visible = true;
            selectAllButton.Visible = true;
            selectNoneButton.Visible = true;
            saveButton.Visible = true;
            searchLabel.Visible = true;
            searchBox.Visible = true;
            clearSearchButton.Visible = true;
            countLabel.Visible = true;
            UpdateCountLabel();
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error: {ex.Message}";
            loadingBar.Visible = false;
        }
    }

    private void PopulateGrid()
    {
        dataGridView.Rows.Clear();

        var filteredItems = string.IsNullOrEmpty(currentSearchText)
            ? items
            : items.Where(i => i.Title.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in filteredItems)
        {
            var rowIndex = dataGridView.Rows.Add(
                item.Selected,
                item.Index,
                item.Title
            );

            dataGridView.Rows[rowIndex].Tag = item;

            // Highlight matching text
            if (!string.IsNullOrEmpty(currentSearchText) &&
                item.Title.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase))
            {
                dataGridView.Rows[rowIndex].Cells["Title"].Style.BackColor = Color.Yellow;
            }
        }

        UpdateCountLabel();
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        currentSearchText = searchBox.Text.Trim();
        PopulateGrid();
    }

    private void UpdateCountLabel()
    {
        int selected = items.Count(i => i.Selected);
        int total = items.Count;
        int shown = dataGridView.Rows.Count;

        if (string.IsNullOrEmpty(currentSearchText))
        {
            countLabel.Text = $"{selected} of {total} selected";
        }
        else
        {
            countLabel.Text = $"Showing {shown} of {total} ({selected} selected)";
        }
    }

    private void DataGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (e.ColumnIndex == 0) // Checkbox column
        {
            var item = dataGridView.Rows[e.RowIndex].Tag as PlaylistItem;
            if (item != null)
            {
                // Toggle will happen after this event, so we invert
                item.Selected = !(bool)dataGridView.Rows[e.RowIndex].Cells[0].Value;
                // Update count after a brief delay to let the checkbox update
                BeginInvoke(() => UpdateCountLabel());
            }
        }
    }

    private void SelectAllButton_Click(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            row.Cells[0].Value = true;
            if (row.Tag is PlaylistItem item)
            {
                item.Selected = true;
            }
        }
        UpdateCountLabel();
    }

    private void SelectNoneButton_Click(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            row.Cells[0].Value = false;
            if (row.Tag is PlaylistItem item)
            {
                item.Selected = false;
            }
        }
        UpdateCountLabel();
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        // Update selections from grid
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (row.Tag is PlaylistItem item)
            {
                item.Selected = (bool)row.Cells[0].Value;
            }
        }

        var selectedCount = items.Count(i => i.Selected);
        if (selectedCount == 0)
        {
            MessageBox.Show("Please select at least one song.", "No Selection",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogConfirmed = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Clean up temporary cookie file
        YouTubeCookieManager.DeleteCookieFile();
        base.OnFormClosing(e);
    }
}
