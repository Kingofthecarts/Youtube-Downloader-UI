namespace Youtube_Downloader;

public class RenameForm : Form
{
    private TextBox filenameTextBox = null!;
    private TextBox albumTextBox = null!;
    private TextBox artistTextBox = null!;
    private TextBox songNameTextBox = null!;
    private TextBox commentsTextBox = null!;
    private Button copyToAllButton = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;
    private SafePictureBox albumArtPictureBox = null!;
    private CheckBox removeAlbumArtCheckBox = null!;
    private Button[] ratingButtons = null!;
    private int currentRating = 0;

    private readonly string originalFilePath;
    private readonly string originalFilename;
    private readonly string? defaultVideoId;
    private readonly bool allowFilenameEdit;
    private readonly List<string> filenameParts;
    private bool hasAlbumArt = false;

    public string NewFilePath { get; private set; } = "";
    public bool ChangesMade { get; private set; } = false;

    // Common separators to split filename by
    private static readonly char[] Separators = { '-', '|', '–', '—', '~', '·' };

    public RenameForm(string filePath, string? videoId = null, bool allowFilenameEdit = false)
    {
        originalFilePath = filePath;
        originalFilename = Path.GetFileNameWithoutExtension(filePath);
        defaultVideoId = videoId;
        this.allowFilenameEdit = allowFilenameEdit;

        // Parse filename into parts based on separators
        filenameParts = ParseFilenameParts(originalFilename);

        InitializeComponents();
        LoadCurrentTags();
    }

    private List<string> ParseFilenameParts(string filename)
    {
        var parts = new List<string>();

        // Split by any separator
        var splitParts = filename.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in splitParts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                parts.Add(trimmed);
            }
        }

        return parts;
    }

    private void InitializeComponents()
    {
        Text = "Rename / Edit Tags";
        Size = new Size(620, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 15;
        int textBoxWidth = 415;
        int leftMargin = 15;
        int copyButtonX = 435;

        // Album Art section (right side)
        var albumArtLabel = new Label
        {
            Text = "Album Art:",
            Location = new Point(485, y),
            AutoSize = true
        };

        albumArtPictureBox = new SafePictureBox
        {
            Location = new Point(485, y + 18),
            Size = new Size(110, 110),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        };

        removeAlbumArtCheckBox = new CheckBox
        {
            Text = "Remove art",
            Location = new Point(485, y + 133),
            AutoSize = true,
            Enabled = false  // Enabled only if there's album art
        };

        // Rating section (under Remove art)
        var ratingLabel = new Label
        {
            Text = "Rating:",
            Location = new Point(485, y + 158),
            AutoSize = true
        };

        ratingButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            int starIndex = i + 1;
            ratingButtons[i] = new Button
            {
                Text = "☆",
                Location = new Point(485 + (i * 22), y + 175),
                Size = new Size(22, 22),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Font.FontFamily, 10),
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                Tag = starIndex
            };
            ratingButtons[i].FlatAppearance.BorderSize = 0;
            ratingButtons[i].Click += RatingButton_Click;
            Controls.Add(ratingButtons[i]);
        }
        Controls.Add(ratingLabel);

        // Filename section
        var filenameLabel = new Label
        {
            Text = "Filename:",
            Location = new Point(leftMargin, y),
            AutoSize = true
        };

        copyToAllButton = new Button
        {
            Text = "Copy to All",
            Location = new Point(300, y - 3),
            Size = new Size(80, 23)
        };
        copyToAllButton.Click += (s, e) =>
        {
            songNameTextBox.Text = filenameTextBox.Text;
            artistTextBox.Text = filenameTextBox.Text;
            albumTextBox.Text = filenameTextBox.Text;
            // Don't copy to comments - preserve video ID or user data
        };
        y += 20;

        filenameTextBox = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(450, 23),
            Text = originalFilename,
            ReadOnly = !allowFilenameEdit,
            BackColor = allowFilenameEdit ? SystemColors.Window : SystemColors.Control
        };
        y += 35;  // Added extra spacing after Copy button area

        // Song Name section
        var songNameLabel = new Label
        {
            Text = "Title (Song Name):",
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        y += 18;

        songNameTextBox = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(textBoxWidth, 23)
        };

        var copySongNameButton = CreateCopyDropdownButton(copyButtonX, y, songNameTextBox);
        y += 32;

        // Album Artist section
        var artistLabel = new Label
        {
            Text = "Album Artist:",
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        y += 18;

        artistTextBox = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(textBoxWidth, 23)
        };

        var copyArtistButton = CreateCopyDropdownButton(copyButtonX, y, artistTextBox);
        y += 32;

        // Album section
        var albumLabel = new Label
        {
            Text = "Album:",
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        y += 18;

        albumTextBox = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(textBoxWidth, 23)
        };

        var copyAlbumButton = CreateCopyDropdownButton(copyButtonX, y, albumTextBox);
        y += 32;

        // Comments section
        var commentsLabel = new Label
        {
            Text = "Comments:",
            Location = new Point(leftMargin, y),
            AutoSize = true
        };
        y += 18;

        commentsTextBox = new TextBox
        {
            Location = new Point(leftMargin, y),
            Size = new Size(textBoxWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        var copyCommentsButton = CreateCopyDropdownButton(copyButtonX, y, commentsTextBox);
        y += 70;

        // Buttons
        saveButton = new Button
        {
            Text = "Save",
            Location = new Point(410, y),
            Size = new Size(85, 30),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += SaveButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(505, y),
            Size = new Size(85, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(albumArtLabel);
        Controls.Add(albumArtPictureBox);
        Controls.Add(removeAlbumArtCheckBox);
        Controls.Add(filenameLabel);
        Controls.Add(filenameTextBox);
        Controls.Add(copyToAllButton);
        Controls.Add(songNameLabel);
        Controls.Add(songNameTextBox);
        Controls.Add(copySongNameButton);
        Controls.Add(artistLabel);
        Controls.Add(artistTextBox);
        Controls.Add(copyArtistButton);
        Controls.Add(albumLabel);
        Controls.Add(albumTextBox);
        Controls.Add(copyAlbumButton);
        Controls.Add(commentsLabel);
        Controls.Add(commentsTextBox);
        Controls.Add(copyCommentsButton);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void LoadCurrentTags()
    {
        try
        {
            using var file = TagLib.File.Create(originalFilePath);
            songNameTextBox.Text = file.Tag.Title ?? "";
            artistTextBox.Text = file.Tag.FirstAlbumArtist ?? "";
            albumTextBox.Text = file.Tag.Album ?? "";

            // Use existing comment, or default to video ID if provided and comment is empty
            var existingComment = file.Tag.Comment ?? "";
            if (string.IsNullOrEmpty(existingComment) && !string.IsNullOrEmpty(defaultVideoId))
            {
                commentsTextBox.Text = defaultVideoId;
            }
            else
            {
                commentsTextBox.Text = existingComment;
            }

            // Load album art
            if (file.Tag.Pictures.Length > 0)
            {
                var picture = file.Tag.Pictures[0];
                // Create a copy so we don't depend on the stream staying open
                using var ms = new MemoryStream(picture.Data.Data);
                using var tempImage = Image.FromStream(ms);
                albumArtPictureBox.Image = new Bitmap(tempImage);
                hasAlbumArt = true;
                removeAlbumArtCheckBox.Enabled = true;
            }

            // Load rating from ID3v2 Popularimeter frame
            if (file.GetTag(TagLib.TagTypes.Id3v2) is TagLib.Id3v2.Tag id3v2Tag)
            {
                var popmFrame = TagLib.Id3v2.PopularimeterFrame.Get(id3v2Tag, "Windows Media Player 9 Series", false);
                if (popmFrame != null && popmFrame.Rating > 0)
                {
                    // Convert 0-255 rating to 1-5 stars
                    // Windows uses: 1=1, 64=2, 128=3, 196=4, 255=5
                    currentRating = popmFrame.Rating switch
                    {
                        >= 224 => 5,
                        >= 160 => 4,
                        >= 96 => 3,
                        >= 32 => 2,
                        >= 1 => 1,
                        _ => 0
                    };
                    UpdateRatingDisplay();
                }
            }
        }
        catch
        {
            // If we can't read tags, leave fields empty
            // But still set video ID as comment if provided
            if (!string.IsNullOrEmpty(defaultVideoId))
            {
                commentsTextBox.Text = defaultVideoId;
            }
        }
    }

    private Button CreateCopyDropdownButton(int x, int y, TextBox targetTextBox)
    {
        var button = new Button
        {
            Text = "▼",
            Location = new Point(x, y),
            Size = new Size(30, 23),
            Font = new Font(Font.FontFamily, 8, FontStyle.Bold)
        };

        button.Click += (s, e) =>
        {
            var menu = new ContextMenuStrip();

            // Add "Copy full filename" option first
            var fullFilenameItem = new ToolStripMenuItem($"Full: {TruncateText(originalFilename, 50)}");
            fullFilenameItem.Click += (_, __) => targetTextBox.Text = originalFilename;
            menu.Items.Add(fullFilenameItem);

            // Add separator if we have parsed parts
            if (filenameParts.Count > 1)
            {
                menu.Items.Add(new ToolStripSeparator());

                // Add each parsed part
                for (int i = 0; i < filenameParts.Count; i++)
                {
                    var part = filenameParts[i];
                    var menuItem = new ToolStripMenuItem($"Part {i + 1}: {TruncateText(part, 45)}");
                    menuItem.Click += (_, __) => targetTextBox.Text = part;
                    menu.Items.Add(menuItem);
                }
            }

            // Show dropdown below the button
            menu.Show(button, new Point(0, button.Height));
        };

        return button;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    private void RatingButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is int starIndex)
        {
            // If clicking the same star that's already selected, clear rating
            if (currentRating == starIndex)
            {
                currentRating = 0;
            }
            else
            {
                currentRating = starIndex;
            }
            UpdateRatingDisplay();
        }
    }

    private void UpdateRatingDisplay()
    {
        for (int i = 0; i < 5; i++)
        {
            ratingButtons[i].Text = i < currentRating ? "★" : "☆";
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        string newFilename = filenameTextBox.Text.Trim();
        string newSongName = songNameTextBox.Text.Trim();
        string newArtist = artistTextBox.Text.Trim();
        string newAlbum = albumTextBox.Text.Trim();
        string newComments = commentsTextBox.Text.Trim();

        if (string.IsNullOrEmpty(newFilename))
        {
            MessageBox.Show("Filename cannot be empty.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        // Remove invalid filename characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            newFilename = newFilename.Replace(c.ToString(), "");
        }

        try
        {
            // Update ID3 tags first (before renaming)
            using (var file = TagLib.File.Create(originalFilePath))
            {
                file.Tag.Title = newSongName;
                file.Tag.AlbumArtists = new[] { newArtist };
                file.Tag.Album = newAlbum;
                file.Tag.Comment = newComments;

                // Remove album art if checkbox is checked
                if (removeAlbumArtCheckBox.Checked && hasAlbumArt)
                {
                    file.Tag.Pictures = Array.Empty<TagLib.IPicture>();
                }

                // Save rating to ID3v2 Popularimeter frame
                if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3v2Tag)
                {
                    var popmFrame = TagLib.Id3v2.PopularimeterFrame.Get(id3v2Tag, "Windows Media Player 9 Series", true);
                    // Convert 1-5 stars to 0-255 rating (Windows Media Player compatible)
                    popmFrame.Rating = currentRating switch
                    {
                        5 => 255,
                        4 => 196,
                        3 => 128,
                        2 => 64,
                        1 => 1,
                        _ => 0
                    };
                }

                file.Save();
            }

            // Rename file if filename changed
            string directory = Path.GetDirectoryName(originalFilePath) ?? "";
            string newPath = Path.Combine(directory, newFilename + ".mp3");

            if (!newPath.Equals(originalFilePath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newPath))
                {
                    MessageBox.Show("A file with that name already exists.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                File.Move(originalFilePath, newPath);
                NewFilePath = newPath;
            }
            else
            {
                NewFilePath = originalFilePath;
            }

            ChangesMade = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save changes: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.None;
        }
    }
}
