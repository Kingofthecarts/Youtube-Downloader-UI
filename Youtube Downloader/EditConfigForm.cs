namespace Youtube_Downloader;

public class EditConfigForm : Form
{
    private TextBox outputFolderTextBox = null!;
    private TextBox tempFolderTextBox = null!;
    private TextBox ytDlpPathTextBox = null!;
    private TextBox ffmpegPathTextBox = null!;
    private TextBox singlesHistoryPathTextBox = null!;
    private TextBox playlistsHistoryPathTextBox = null!;
    private TextBox logFolderTextBox = null!;
    private Label diskSpaceLabel = null!;
    private TextBox ytDlpUrlTextBox = null!;
    private TextBox ffmpegUrlTextBox = null!;
    private TextBox denoUrlTextBox = null!;
    private TextBox webView2UrlTextBox = null!;
    private Button ytDlpUrlResetButton = null!;
    private Button ffmpegUrlResetButton = null!;
    private Button denoUrlResetButton = null!;
    private Button webView2UrlResetButton = null!;
    private Button outputFolderBrowseButton = null!;
    private Button outputFolderGotoButton = null!;
    private Button tempFolderBrowseButton = null!;
    private Button tempFolderGotoButton = null!;
    private Button ytDlpBrowseButton = null!;
    private Button ffmpegBrowseButton = null!;
    private Button ytDlpGotoButton = null!;
    private Button ffmpegGotoButton = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;
    private Button resetConfigButton = null!;
    private Button editInNotepadButton = null!;
    private Label outputFolderStatusLabel = null!;
    private Label tempFolderStatusLabel = null!;
    private Label ytDlpStatusLabel = null!;
    private Label ffmpegStatusLabel = null!;
    private TextBox configLocationTextBox = null!;
    private Button moveConfigButton = null!;

    private readonly ToolsManager toolsManager;
    private readonly Config config;
    private readonly DownloadHistory history;
    private string originalOutputFolder;
    private string originalTempFolder;
    private string originalYtDlpPath;
    private string originalFfmpegPath;
    private string originalYtDlpUrl;
    private string originalFfmpegUrl;
    private string originalDenoUrl;
    private string originalWebView2Url;

    public bool ConfigChanged { get; private set; } = false;
    public string NewOutputFolder { get; private set; } = "";
    public bool RestartRequested { get; private set; } = false;

    public EditConfigForm(ToolsManager toolsManager, Config config, DownloadHistory history)
    {
        this.toolsManager = toolsManager;
        this.config = config;
        this.history = history;
        originalOutputFolder = config.OutputFolder;
        originalTempFolder = config.TempFolder;
        originalYtDlpPath = toolsManager.YtDlpPath;
        originalFfmpegPath = toolsManager.FfmpegPath;
        originalYtDlpUrl = config.YtDlpDownloadUrl;
        originalFfmpegUrl = config.FfmpegDownloadUrl;
        originalDenoUrl = config.DenoDownloadUrl;
        originalWebView2Url = config.WebView2DownloadUrl;

        InitializeComponents();
        UpdateStatusLabels();
    }

    private void InitializeComponents()
    {
        Text = "Edit Configuration";
        Size = new Size(600, 1000);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 20;

        // Output Folder section
        var outputFolderLabel = new Label
        {
            Text = "Output Folder:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        outputFolderTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = config.OutputFolder
        };
        outputFolderTextBox.TextChanged += (s, e) => UpdateStatusLabels();

        outputFolderBrowseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(420, y - 1),
            Size = new Size(75, 25)
        };
        outputFolderBrowseButton.Click += OutputFolderBrowseButton_Click;

        outputFolderGotoButton = new Button
        {
            Text = "Go To",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        outputFolderGotoButton.Click += OutputFolderGotoButton_Click;

        y += 28;

        outputFolderStatusLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(555, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        // Temp Folder section
        var tempFolderLabel = new Label
        {
            Text = "Temp Folder (downloads & converts here first):",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        tempFolderTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = config.TempFolder
        };
        tempFolderTextBox.TextChanged += (s, e) => UpdateStatusLabels();

        tempFolderBrowseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(420, y - 1),
            Size = new Size(75, 25)
        };
        tempFolderBrowseButton.Click += TempFolderBrowseButton_Click;

        tempFolderGotoButton = new Button
        {
            Text = "Go To",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        tempFolderGotoButton.Click += TempFolderGotoButton_Click;

        y += 28;

        tempFolderStatusLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(555, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        // yt-dlp section
        var ytDlpLabel = new Label
        {
            Text = "yt-dlp.exe Location:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        ytDlpPathTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = toolsManager.YtDlpPath
        };
        ytDlpPathTextBox.TextChanged += (s, e) => UpdateStatusLabels();

        ytDlpBrowseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(420, y - 1),
            Size = new Size(75, 25)
        };
        ytDlpBrowseButton.Click += YtDlpBrowseButton_Click;

        ytDlpGotoButton = new Button
        {
            Text = "Go To",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        ytDlpGotoButton.Click += YtDlpGotoButton_Click;

        y += 28;

        ytDlpStatusLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(555, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        // ffmpeg section
        var ffmpegLabel = new Label
        {
            Text = "ffmpeg bin folder (containing ffmpeg.exe):",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 22;

        ffmpegPathTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = toolsManager.FfmpegPath
        };
        ffmpegPathTextBox.TextChanged += (s, e) => UpdateStatusLabels();

        ffmpegBrowseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(420, y - 1),
            Size = new Size(75, 25)
        };
        ffmpegBrowseButton.Click += FfmpegBrowseButton_Click;

        ffmpegGotoButton = new Button
        {
            Text = "Go To",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        ffmpegGotoButton.Click += FfmpegGotoButton_Click;

        y += 28;

        ffmpegStatusLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(555, 20),
            ForeColor = Color.Gray
        };
        y += 35;

        // Download URLs section
        var urlSectionLabel = new Label
        {
            Text = "Download URLs (for auto-download of tools):",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        var ytDlpUrlLabel = new Label
        {
            Text = "yt-dlp URL:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 20;

        ytDlpUrlTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(480, 23),
            Text = config.YtDlpDownloadUrl
        };

        ytDlpUrlResetButton = new Button
        {
            Text = "Reset",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        ytDlpUrlResetButton.Click += (s, e) => { ytDlpUrlTextBox.Text = Config.DefaultYtDlpUrl; };
        y += 28;

        var ffmpegUrlLabel = new Label
        {
            Text = "ffmpeg URL:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 20;

        ffmpegUrlTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(480, 23),
            Text = config.FfmpegDownloadUrl
        };

        ffmpegUrlResetButton = new Button
        {
            Text = "Reset",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        ffmpegUrlResetButton.Click += (s, e) => { ffmpegUrlTextBox.Text = Config.DefaultFfmpegUrl; };
        y += 28;

        var denoUrlLabel = new Label
        {
            Text = "Deno URL:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 20;

        denoUrlTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(480, 23),
            Text = config.DenoDownloadUrl
        };

        denoUrlResetButton = new Button
        {
            Text = "Reset",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        denoUrlResetButton.Click += (s, e) => { denoUrlTextBox.Text = Config.DefaultDenoUrl; };
        y += 28;

        var webView2UrlLabel = new Label
        {
            Text = "WebView2 URL:",
            Location = new Point(15, y),
            AutoSize = true
        };
        y += 20;

        webView2UrlTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(480, 23),
            Text = config.WebView2DownloadUrl
        };

        webView2UrlResetButton = new Button
        {
            Text = "Reset",
            Location = new Point(500, y - 1),
            Size = new Size(70, 25)
        };
        webView2UrlResetButton.Click += (s, e) => { webView2UrlTextBox.Text = Config.DefaultWebView2Url; };
        y += 35;

        // History Files section (read-only)
        var historyLabel = new Label
        {
            Text = "History Files (read-only):",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        var singlesHistoryLabel = new Label
        {
            Text = "Singles History:",
            Location = new Point(15, y),
            AutoSize = true
        };

        singlesHistoryPathTextBox = new TextBox
        {
            Location = new Point(120, y - 2),
            Size = new Size(450, 23),
            Text = history.SinglesHistoryPath,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };
        y += 28;

        var playlistsHistoryLabel = new Label
        {
            Text = "Playlists History:",
            Location = new Point(15, y),
            AutoSize = true
        };

        playlistsHistoryPathTextBox = new TextBox
        {
            Location = new Point(120, y - 2),
            Size = new Size(450, 23),
            Text = history.PlaylistsHistoryPath,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };
        y += 28;

        var logFolderLabel = new Label
        {
            Text = "Log Folder:",
            Location = new Point(15, y),
            AutoSize = true
        };

        logFolderTextBox = new TextBox
        {
            Location = new Point(120, y - 2),
            Size = new Size(450, 23),
            Text = config.LogFolder,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };
        y += 28;

        var folderHistoryLabel = new Label
        {
            Text = "Folder List:",
            Location = new Point(15, y),
            AutoSize = true
        };

        var folderHistoryTextBox = new TextBox
        {
            Location = new Point(120, y - 2),
            Size = new Size(450, 23),
            Text = config.ResolvedFolderHistoryPath,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };
        y += 35;

        Controls.Add(logFolderLabel);
        Controls.Add(logFolderTextBox);
        Controls.Add(folderHistoryLabel);
        Controls.Add(folderHistoryTextBox);

        // Disk Space section (read-only)
        var diskSpaceSectionLabel = new Label
        {
            Text = "Disk Space (read-only):",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        diskSpaceLabel = new Label
        {
            Location = new Point(15, y),
            Size = new Size(555, 20),
            Text = GetDiskSpaceInfo()
        };
        y += 35;

        Controls.Add(diskSpaceSectionLabel);
        Controls.Add(diskSpaceLabel);

        // Config Location section
        var configLocationSectionLabel = new Label
        {
            Text = "Config File Location:",
            Location = new Point(15, y),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };
        y += 22;

        configLocationTextBox = new TextBox
        {
            Location = new Point(15, y),
            Size = new Size(400, 23),
            Text = config.ConfigLocation,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };

        moveConfigButton = new Button
        {
            Text = config.IsStoredBesideApp ? "Move to AppData" : "Move to App Folder",
            Location = new Point(420, y - 1),
            Size = new Size(150, 25)
        };
        moveConfigButton.Click += MoveConfigButton_Click;
        y += 30;

        resetConfigButton = new Button
        {
            Text = "Reset Configuration...",
            Location = new Point(15, y),
            Size = new Size(150, 25),
            ForeColor = Color.DarkRed
        };
        resetConfigButton.Click += ResetConfigButton_Click;

        editInNotepadButton = new Button
        {
            Text = "Edit in Notepad",
            Location = new Point(175, y),
            Size = new Size(120, 25)
        };
        editInNotepadButton.Click += EditInNotepadButton_Click;
        y += 35;

        Controls.Add(configLocationSectionLabel);
        Controls.Add(configLocationTextBox);
        Controls.Add(moveConfigButton);
        Controls.Add(resetConfigButton);
        Controls.Add(editInNotepadButton);

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

        // Add controls in visual order
        Controls.Add(outputFolderLabel);
        Controls.Add(outputFolderTextBox);
        Controls.Add(outputFolderBrowseButton);
        Controls.Add(outputFolderGotoButton);
        Controls.Add(outputFolderStatusLabel);
        Controls.Add(tempFolderLabel);
        Controls.Add(tempFolderTextBox);
        Controls.Add(tempFolderBrowseButton);
        Controls.Add(tempFolderGotoButton);
        Controls.Add(tempFolderStatusLabel);
        Controls.Add(ytDlpLabel);
        Controls.Add(ytDlpPathTextBox);
        Controls.Add(ytDlpBrowseButton);
        Controls.Add(ytDlpGotoButton);
        Controls.Add(ytDlpStatusLabel);
        Controls.Add(ffmpegLabel);
        Controls.Add(ffmpegPathTextBox);
        Controls.Add(ffmpegBrowseButton);
        Controls.Add(ffmpegGotoButton);
        Controls.Add(ffmpegStatusLabel);
        Controls.Add(urlSectionLabel);
        Controls.Add(ytDlpUrlLabel);
        Controls.Add(ytDlpUrlTextBox);
        Controls.Add(ytDlpUrlResetButton);
        Controls.Add(ffmpegUrlLabel);
        Controls.Add(ffmpegUrlTextBox);
        Controls.Add(ffmpegUrlResetButton);
        Controls.Add(denoUrlLabel);
        Controls.Add(denoUrlTextBox);
        Controls.Add(denoUrlResetButton);
        Controls.Add(webView2UrlLabel);
        Controls.Add(webView2UrlTextBox);
        Controls.Add(webView2UrlResetButton);
        Controls.Add(historyLabel);
        Controls.Add(singlesHistoryLabel);
        Controls.Add(singlesHistoryPathTextBox);
        Controls.Add(playlistsHistoryLabel);
        Controls.Add(playlistsHistoryPathTextBox);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
    }

    private void UpdateStatusLabels()
    {
        // Output folder status
        string outputFolder = outputFolderTextBox.Text.Trim();
        if (string.IsNullOrEmpty(outputFolder))
        {
            outputFolderStatusLabel.Text = "Path not set";
            outputFolderStatusLabel.ForeColor = Color.Red;
            outputFolderGotoButton.Enabled = false;
        }
        else if (Directory.Exists(outputFolder))
        {
            outputFolderStatusLabel.Text = "Folder exists";
            outputFolderStatusLabel.ForeColor = Color.Green;
            outputFolderGotoButton.Enabled = true;
        }
        else
        {
            outputFolderStatusLabel.Text = "Folder not found (will be created)";
            outputFolderStatusLabel.ForeColor = Color.Orange;
            outputFolderGotoButton.Enabled = false;
        }

        // Temp folder status
        string tempFolder = tempFolderTextBox.Text.Trim();
        if (string.IsNullOrEmpty(tempFolder))
        {
            tempFolderStatusLabel.Text = "Path not set";
            tempFolderStatusLabel.ForeColor = Color.Red;
            tempFolderGotoButton.Enabled = false;
        }
        else if (Directory.Exists(tempFolder))
        {
            tempFolderStatusLabel.Text = "Folder exists";
            tempFolderStatusLabel.ForeColor = Color.Green;
            tempFolderGotoButton.Enabled = true;
        }
        else
        {
            tempFolderStatusLabel.Text = "Folder not found (will be created)";
            tempFolderStatusLabel.ForeColor = Color.Orange;
            tempFolderGotoButton.Enabled = false;
        }

        // yt-dlp status
        string ytDlpPath = ytDlpPathTextBox.Text.Trim();
        if (string.IsNullOrEmpty(ytDlpPath))
        {
            ytDlpStatusLabel.Text = "Path not set";
            ytDlpStatusLabel.ForeColor = Color.Red;
            ytDlpGotoButton.Enabled = false;
        }
        else if (File.Exists(ytDlpPath))
        {
            ytDlpStatusLabel.Text = "File found";
            ytDlpStatusLabel.ForeColor = Color.Green;
            ytDlpGotoButton.Enabled = true;
        }
        else
        {
            ytDlpStatusLabel.Text = "File not found";
            ytDlpStatusLabel.ForeColor = Color.Red;
            ytDlpGotoButton.Enabled = false;
        }

        // ffmpeg status
        string ffmpegPath = ffmpegPathTextBox.Text.Trim();
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            ffmpegStatusLabel.Text = "Path not set";
            ffmpegStatusLabel.ForeColor = Color.Red;
            ffmpegGotoButton.Enabled = false;
        }
        else if (Directory.Exists(ffmpegPath) && File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
        {
            ffmpegStatusLabel.Text = "ffmpeg.exe found";
            ffmpegStatusLabel.ForeColor = Color.Green;
            ffmpegGotoButton.Enabled = true;
        }
        else if (Directory.Exists(ffmpegPath))
        {
            ffmpegStatusLabel.Text = "Folder exists but ffmpeg.exe not found";
            ffmpegStatusLabel.ForeColor = Color.Orange;
            ffmpegGotoButton.Enabled = true;
        }
        else
        {
            ffmpegStatusLabel.Text = "Folder not found";
            ffmpegStatusLabel.ForeColor = Color.Red;
            ffmpegGotoButton.Enabled = false;
        }
    }

    private void OutputFolderBrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the output folder for downloads",
            ShowNewFolderButton = true
        };

        string currentPath = outputFolderTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
        {
            dialog.SelectedPath = currentPath;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            outputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OutputFolderGotoButton_Click(object? sender, EventArgs e)
    {
        string path = outputFolderTextBox.Text.Trim();
        if (Directory.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }

    private void TempFolderBrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the temp folder for downloads",
            ShowNewFolderButton = true
        };

        string currentPath = tempFolderTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
        {
            dialog.SelectedPath = currentPath;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            tempFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void TempFolderGotoButton_Click(object? sender, EventArgs e)
    {
        string path = tempFolderTextBox.Text.Trim();
        if (Directory.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }

    private void YtDlpBrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select yt-dlp.exe",
            Filter = "yt-dlp executable|yt-dlp.exe|All executables|*.exe",
            CheckFileExists = true
        };

        string currentPath = ytDlpPathTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
            dialog.FileName = Path.GetFileName(currentPath);
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ytDlpPathTextBox.Text = dialog.FileName;
        }
    }

    private void FfmpegBrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the ffmpeg bin folder (containing ffmpeg.exe)",
            ShowNewFolderButton = false
        };

        string currentPath = ffmpegPathTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
        {
            dialog.SelectedPath = currentPath;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ffmpegPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void YtDlpGotoButton_Click(object? sender, EventArgs e)
    {
        string path = ytDlpPathTextBox.Text.Trim();
        if (File.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
    }

    private void FfmpegGotoButton_Click(object? sender, EventArgs e)
    {
        string path = ffmpegPathTextBox.Text.Trim();
        if (Directory.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }

    private string GetDiskSpaceInfo()
    {
        try
        {
            string outputPath = config.OutputFolder;
            if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(outputPath)!);
                double freePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100;
                long freeGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                long totalGB = driveInfo.TotalSize / (1024 * 1024 * 1024);
                return $"{driveInfo.Name} {freePercent:F1}% free ({freeGB} GB free of {totalGB} GB)";
            }
            return "Output folder not set or not found";
        }
        catch
        {
            return "Unable to determine disk space";
        }
    }

    private void MoveConfigButton_Click(object? sender, EventArgs e)
    {
        string targetPath = config.IsStoredBesideApp ? config.AppDataConfigPath : config.AppSideConfigPath;
        string targetDescription = config.IsStoredBesideApp ? "AppData" : "App Folder (portable)";

        var result = MessageBox.Show(
            $"Move config file to {targetDescription}?\n\n" +
            $"From:\n{config.ConfigLocation}\n\n" +
            $"To:\n{targetPath}",
            "Move Config File",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            if (config.MoveConfigLocation())
            {
                configLocationTextBox.Text = config.ConfigLocation;
                UpdateMoveButtonText();
                MessageBox.Show(
                    $"Config file moved successfully.\n\nNew location:\n{config.ConfigLocation}",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Failed to move config file. Please check folder permissions.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    private void UpdateMoveButtonText()
    {
        if (config.IsStoredBesideApp)
        {
            moveConfigButton.Text = "Move to AppData";
        }
        else
        {
            moveConfigButton.Text = "Move to App Folder";
        }
    }

    private void ResetConfigButton_Click(object? sender, EventArgs e)
    {
        // Build paths for both config locations
        string appDataConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeDownloader",
            "config.xml");
        string appSideConfigPath = Path.Combine(AppPaths.AppDirectory, "config.xml");
        string sourceFolderPath = Path.Combine(AppPaths.AppDirectory, "Source");

        bool appDataExists = File.Exists(appDataConfigPath);
        bool appSideExists = File.Exists(appSideConfigPath);
        bool sourceFolderExists = Directory.Exists(sourceFolderPath);

        string message = "This will reset the application to first-run state.\n\n";

        if (appDataExists || appSideExists || sourceFolderExists)
        {
            message += "The following will be deleted:\n";
            if (appDataExists)
                message += $"  - {appDataConfigPath}\n";
            if (appSideExists)
                message += $"  - {appSideConfigPath}\n";
            if (sourceFolderExists)
                message += $"  - {sourceFolderPath} (downloaded tools)\n";
            message += "\n";
        }

        message += "The application will restart after reset.\n\nAre you sure you want to continue?";

        var result = MessageBox.Show(
            message,
            "Reset Configuration",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        // Delete AppData config if exists
        if (appDataExists)
        {
            try
            {
                File.Delete(appDataConfigPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete AppData config:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        // Delete app-side config if exists
        if (appSideExists)
        {
            try
            {
                File.Delete(appSideConfigPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete app-side config:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        // Delete Source folder if exists (contains downloaded tools like ffmpeg, yt-dlp)
        if (sourceFolderExists)
        {
            try
            {
                Directory.Delete(sourceFolderPath, recursive: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete Source folder:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        // Signal that restart is needed
        RestartRequested = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void EditInNotepadButton_Click(object? sender, EventArgs e)
    {
        if (File.Exists(config.ConfigLocation))
        {
            try
            {
                System.Diagnostics.Process.Start("notepad.exe", config.ConfigLocation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open Notepad:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        else
        {
            MessageBox.Show(
                "Config file does not exist yet.\nSave the configuration first.",
                "File Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        string outputFolder = outputFolderTextBox.Text.Trim();
        string tempFolder = tempFolderTextBox.Text.Trim();
        string ytDlpPath = ytDlpPathTextBox.Text.Trim();
        string ffmpegPath = ffmpegPathTextBox.Text.Trim();
        string ytDlpUrl = ytDlpUrlTextBox.Text.Trim();
        string ffmpegUrl = ffmpegUrlTextBox.Text.Trim();
        string denoUrl = denoUrlTextBox.Text.Trim();
        string webView2Url = webView2UrlTextBox.Text.Trim();

        // Validate paths
        List<string> errors = new();

        if (string.IsNullOrEmpty(outputFolder))
        {
            errors.Add("Output folder is not set");
        }

        if (string.IsNullOrEmpty(tempFolder))
        {
            errors.Add("Temp folder is not set");
        }

        if (string.IsNullOrEmpty(ytDlpPath))
        {
            errors.Add("yt-dlp path is not set");
        }
        else if (!File.Exists(ytDlpPath))
        {
            errors.Add("yt-dlp.exe file not found");
        }

        if (string.IsNullOrEmpty(ffmpegPath))
        {
            errors.Add("ffmpeg path is not set");
        }
        else if (!Directory.Exists(ffmpegPath))
        {
            errors.Add("ffmpeg folder not found");
        }
        else if (!File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
        {
            errors.Add("ffmpeg.exe not found in the specified folder");
        }

        if (string.IsNullOrEmpty(ytDlpUrl))
        {
            errors.Add("yt-dlp download URL is not set");
        }

        if (string.IsNullOrEmpty(ffmpegUrl))
        {
            errors.Add("ffmpeg download URL is not set");
        }

        if (string.IsNullOrEmpty(denoUrl))
        {
            errors.Add("Deno download URL is not set");
        }

        if (string.IsNullOrEmpty(webView2Url))
        {
            errors.Add("WebView2 download URL is not set");
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(
                "Cannot save configuration:\n\n" + string.Join("\n", errors.Select(err => $"- {err}")),
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Build list of changes
        var changes = new List<string>();

        if (outputFolder != originalOutputFolder)
            changes.Add($"Output Folder: {originalOutputFolder} -> {outputFolder}");
        if (tempFolder != originalTempFolder)
            changes.Add($"Temp Folder: {originalTempFolder} -> {tempFolder}");
        if (ytDlpPath != originalYtDlpPath)
            changes.Add($"yt-dlp Path: {originalYtDlpPath} -> {ytDlpPath}");
        if (ffmpegPath != originalFfmpegPath)
            changes.Add($"ffmpeg Path: {originalFfmpegPath} -> {ffmpegPath}");
        if (ytDlpUrl != originalYtDlpUrl)
            changes.Add($"yt-dlp URL: Changed");
        if (ffmpegUrl != originalFfmpegUrl)
            changes.Add($"ffmpeg URL: Changed");
        if (denoUrl != originalDenoUrl)
            changes.Add($"Deno URL: Changed");
        if (webView2Url != originalWebView2Url)
            changes.Add($"WebView2 URL: Changed");

        if (changes.Count == 0)
        {
            MessageBox.Show("No changes were made.", "No Changes",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        // Show change preview dialog
        using var previewForm = new ConfigChangePreviewForm(changes);
        if (previewForm.ShowDialog(this) == DialogResult.OK)
        {
            // Create output folder if it doesn't exist
            if (!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create output folder:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Create temp folder if it doesn't exist
            if (!Directory.Exists(tempFolder))
            {
                try
                {
                    Directory.CreateDirectory(tempFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create temp folder:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Save folders and URLs
            config.OutputFolder = outputFolder;
            config.TempFolder = tempFolder;
            config.YtDlpDownloadUrl = ytDlpUrl;
            config.FfmpegDownloadUrl = ffmpegUrl;
            config.DenoDownloadUrl = denoUrl;
            config.WebView2DownloadUrl = webView2Url;
            config.Save();
            NewOutputFolder = outputFolder;

            // Save tool paths
            toolsManager.SetYtDlpPath(ytDlpPath);
            toolsManager.SetFfmpegPath(ffmpegPath);
            ConfigChanged = true;

            MessageBox.Show("Configuration saved successfully.", "Saved",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

public class ConfigChangePreviewForm : Form
{
    public ConfigChangePreviewForm(List<string> changes)
    {
        Text = "Confirm Configuration Changes";
        Size = new Size(500, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var headerLabel = new Label
        {
            Text = "The following changes will be saved:",
            Location = new Point(15, 15),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
        };

        var changesTextBox = new TextBox
        {
            Location = new Point(15, 40),
            Size = new Size(455, 200),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = SystemColors.Window,
            Text = string.Join(Environment.NewLine + Environment.NewLine, changes)
        };

        var confirmButton = new Button
        {
            Text = "Confirm",
            Location = new Point(290, 260),
            Size = new Size(85, 30),
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(385, 260),
            Size = new Size(85, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(headerLabel);
        Controls.Add(changesTextBox);
        Controls.Add(confirmButton);
        Controls.Add(cancelButton);

        AcceptButton = confirmButton;
        CancelButton = cancelButton;
    }
}
