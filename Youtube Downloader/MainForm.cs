using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Youtube_Downloader;

/// <summary>
/// Main form for the YouTube Downloader application.
/// This partial class contains field declarations, constructor, and lifecycle methods.
///
/// Related partial class files:
/// - MainForm.UI.cs: UI initialization methods
/// - MainForm.Download.cs: Download logic and yt-dlp integration
/// - MainForm.ChannelMonitor.cs: Channel monitoring functionality
/// - MainForm.OneDrive.cs: OneDrive monitoring functionality
/// - MainForm.Helpers.cs: Utility methods, timers, and menu handlers
/// </summary>
public partial class MainForm : Form
{
    // Compiled regex patterns to avoid allocations on every output line
    private static readonly Regex PlaylistItemRegex = new(@"\[download\] Downloading item (\d+) of (\d+)", RegexOptions.Compiled);
    private static readonly Regex VideoIdRegex = new(@"\[youtube\] ([a-zA-Z0-9_-]{11}):", RegexOptions.Compiled);
    private static readonly Regex ProgressRegex = new(@"\[download\]\s+(\d+\.?\d*)%\s+of\s+~?(\d+\.?\d*)(Ki?B|Mi?B|Gi?B)", RegexOptions.Compiled);
    private static readonly Regex SimpleProgressRegex = new(@"\[download\]\s+(\d+\.?\d*)%", RegexOptions.Compiled);
    private static readonly Regex DownloadDestRegex = new(@"\[download\] Destination: (.+\.(webm|m4a|mp4|opus))", RegexOptions.Compiled);
    private static readonly Regex FfmpegSizeRegex = new(@"size=\s*(\d+)(kB|mB|KB|MB)", RegexOptions.Compiled);
    private static readonly Regex FfmpegTimeRegex = new(@"time=(\d+):(\d+):(\d+\.?\d*)", RegexOptions.Compiled);
    private static readonly Regex ExtractAudioDestRegex = new(@"\[ExtractAudio\] Destination: (.+\.mp3)", RegexOptions.Compiled);

    // Single video controls
    private TextBox urlTextBox = null!;
    private ComboBox singleFolderComboBox = null!;
    private Button singleClearFolderButton = null!;
    private Button goButton = null!;

    // Playlist controls
    private TextBox playlistUrlTextBox = null!;
    private ComboBox playlistFolderComboBox = null!;
    private Button playlistGoButton = null!;
    private Button playlistBrowseButton = null!;
    private CheckBox trackEachSongCheckBox = null!;
    private CheckBox trackChannelPlaylistCheckBox = null!;
    private CheckBox allowRenameCheckBox = null!;
    private CheckBox trackChannelSingleCheckBox = null!;
    private List<PlaylistItem>? selectedPlaylistItems = null;
    private List<PlaylistItem>? excludedPlaylistItems = null;

    // Shared controls
    private Button cancelButton = null!;
    private Button clearAllButton = null!;
    private ProgressBar downloadProgressBar = null!;
    private ProgressBar convertProgressBar = null!;
    private ProgressBar playlistProgressBar = null!;
    private Label playlistProgressLabel = null!;
    private Label downloadLabel = null!;
    private Label convertLabel = null!;
    private Label playlistLabel = null!;
    private Label statusLabel = null!;
    private LinkLabel sourceLink = null!;
    private LinkLabel destinationLink = null!;
    private Button playSongButton = null!;
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem youtubeSignInMenuItem = null!;
    private ToolStripMenuItem youtubeSignOutMenuItem = null!;
    private ToolStripStatusLabel youtubeStatusLabel = null!;

    // Download process tracking
    private Process? currentProcess = null;
    private CancellationTokenSource? cancellationTokenSource = null;
    private bool isCancelling = false;
    private List<string> filesToCleanup = new();

    private string ytDlpPath = "";
    private string ffmpegPath = "";
    private readonly Config config;
    private readonly DownloadHistory history;
    private readonly Logger logger;
    private readonly ToolsManager toolsManager;
    private readonly FolderHistory folderHistory;
    private string? lastDownloadedFile;
    private string? currentVideoTitle;
    private string? currentVideoId;
    private bool isPlaylistDownload = false;
    private int playlistTotal = 0;
    private int playlistCurrent = 0;
    private string? currentPlaylistUrl;
    private string? currentPlaylistFolder;
    private string? currentPlaylistTitle;
    private List<DownloadRecord> playlistRecords = new();
    private double currentVideoDurationSeconds = 0;
    private string? currentChannelId;
    private string? currentChannelName;
    private long downloadedFileSize = 0;
    private System.Windows.Forms.Timer? convertProgressTimer;
    private bool isConverting = false;
    private bool isDownloading = false;
    private string lastYtDlpOutput = ""; // For auth error detection

    // Timing tracking
    private DateTime jobStartTime;
    private DateTime downloadStartTime;
    private DateTime convertStartTime;
    private TimeSpan totalDownloadTime;
    private TimeSpan totalConvertTime;
    private TimeSpan currentVideoDownloadTime;
    private TimeSpan currentVideoConvertTime;
    private System.Windows.Forms.Timer? jobTimer;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel downloadTimeLabel = null!;
    private ToolStripStatusLabel convertTimeLabel = null!;
    private ToolStripStatusLabel totalTimeLabel = null!;
    private ToolStripStatusLabel cpuLabel = null!;
    private ToolStripStatusLabel memoryLabel = null!;
    private ToolStripStatusLabel driveLabel = null!;
    private System.Windows.Forms.Timer? statsTimer;
    private PerformanceCounter? cpuCounter;
    private PerformanceCounter? availableMemCounter;
    private long totalPhysicalMemory;

    // Track open Song Browser to allow only one instance
    private SongBrowserForm? openSongBrowser;

    // Track if Options or EditConfig dialogs are open (prevent simultaneous)
    private bool isOptionsOrEditConfigOpen = false;

    // Tab control for Single/Playlist
    private TabControl downloadTabControl = null!;

    // OneDrive monitoring
    private System.Windows.Forms.Timer? oneDriveTimer;

    // Channel Monitor
    private ChannelMonitorStorage? channelMonitorStorage;
    private ChannelMonitorForm? openChannelMonitor;
    private System.Windows.Forms.Timer? channelScanTimer;
    private DateTime? lastChannelScanTime;
    private Panel? channelAlertPanel;
    private Label? channelAlertLabel;

    // Download statistics
    private readonly DownloadStats downloadStats;

    // Media player controls on main form (mirrors SongBrowser player)
    private Panel? mediaPlayerPanel;
    private Label? playerTitleLabel;
    private Label? playerTimeLabel;
    private TrackBar? playerSeekBar;
    private Button? playerPlayPauseButton;
    private Button? playerStopButton;
    private TrackBar? playerVolumeBar;
    private SafePictureBox? playerAlbumArt;

    public MainForm()
    {
        config = new Config();
        history = new DownloadHistory();
        downloadStats = new DownloadStats();
        logger = new Logger(config.LogFolder);
        toolsManager = new ToolsManager(config);
        folderHistory = new FolderHistory(config.ResolvedFolderHistoryPath);

        logger.Log("Application started");
        logger.Log($"Output folder: {config.OutputFolder}");

        InitializeComponents();

        // Initialize convert progress timer
        convertProgressTimer = new System.Windows.Forms.Timer();
        convertProgressTimer.Interval = 500; // Check every 500ms
        convertProgressTimer.Tick += ConvertProgressTimer_Tick;

        // Initialize job timer for updating time display
        jobTimer = new System.Windows.Forms.Timer();
        jobTimer.Interval = 1000; // Update every second
        jobTimer.Tick += JobTimer_Tick;

        // Initialize stats timer for CPU/memory/drive (runs always)
        statsTimer = new System.Windows.Forms.Timer();
        statsTimer.Interval = 2000; // Update every 2 seconds
        statsTimer.Tick += StatsTimer_Tick;

        // Initialize performance counters once (avoid creating them repeatedly)
        try
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            availableMemCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            cpuCounter.NextValue(); // First call returns 0, prime it

            // Get total physical memory
            var memInfo = GC.GetGCMemoryInfo();
            totalPhysicalMemory = memInfo.TotalAvailableMemoryBytes;
        }
        catch
        {
            // Performance counters may not be available on all systems
        }

        statsTimer.Start();

        // Check for tools after form is shown
        Load += MainForm_Load;

        // Initialize folder dropdowns
        RefreshFolderDropdowns();
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        // Handle first run - prompt for output folder
        if (config.IsFirstRun || string.IsNullOrEmpty(config.OutputFolder))
        {
            // Ensure form is visible and on top before showing dialogs
            this.BringToFront();
            this.TopMost = true;
            this.Activate();

            MessageBox.Show(
                this,
                "Welcome to YouTube Downloader!\n\n" +
                "Please select a folder where your downloaded MP3 files will be saved.",
                "First Run Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using var dialog = new FolderBrowserDialog
            {
                Description = "Select output folder for downloads",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                config.OutputFolder = dialog.SelectedPath;
                config.Save();
                logger.Log($"Output folder set to: {config.OutputFolder}");
            }
            else
            {
                MessageBox.Show(
                    this,
                    "An output folder is required.\n\nThe application will now close.",
                    "Setup Cancelled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Application.Exit();
                return;
            }
        }

        // Ensure temp folder exists
        if (!Directory.Exists(config.TempFolder))
        {
            try
            {
                Directory.CreateDirectory(config.TempFolder);
                logger.Log($"Created temp folder: {config.TempFolder}");
            }
            catch (Exception ex)
            {
                logger.Log($"Failed to create temp folder: {ex.Message}");
            }
        }

        // Ask about OneDrive monitoring on first run (or if it was never set)
        if (config.IsFirstRun)
        {
            // Ensure form is visible and on top for first run dialogs
            this.BringToFront();
            this.TopMost = true;
            this.Activate();

            var oneDriveResult = MessageBox.Show(
                this,
                "Would you like to enable OneDrive monitoring?\n\n" +
                "This will check if OneDrive is running when the app starts,\n" +
                "and every 5 minutes while running.\n\n" +
                "You'll be warned if OneDrive stops running.",
                "OneDrive Monitoring",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            config.MonitorOneDrive = (oneDriveResult == DialogResult.Yes);
            config.Save();
            logger.Log($"OneDrive monitoring set to: {config.MonitorOneDrive}");

            // Ask about minimize on close behavior
            var minimizeResult = MessageBox.Show(
                this,
                "Would you like the app to minimize to taskbar when you click the X button?\n\n" +
                "Yes = Minimize (app stays running)\n" +
                "No = Close the app completely",
                "Close Button Behavior",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            config.MinimizeOnClose = (minimizeResult == DialogResult.Yes);
            config.Save();
            logger.Log($"Minimize on close set to: {config.MinimizeOnClose}");

            // Check if WebView2 is installed (required for YouTube sign-in)
            if (!ToolsManager.IsWebView2Available())
            {
                var webViewResult = MessageBox.Show(
                    this,
                    "WebView2 Runtime is not installed.\n\n" +
                    "WebView2 is required for YouTube sign-in functionality.\n" +
                    "It's a Microsoft component that provides embedded browser capabilities.\n\n" +
                    "Would you like to install it now?\n\n" +
                    "Note: This requires administrator privileges.",
                    "WebView2 Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (webViewResult == DialogResult.Yes)
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
                        logger.Log("WebView2 Runtime installed during first-run setup");
                    }
                    else if (!downloadForm.WasCancelled)
                    {
                        logger.Log("WebView2 installation failed during first-run setup");
                    }
                }
            }

            // Ask about YouTube sign-in (only if WebView2 is now available)
            if (ToolsManager.IsWebView2Available())
            {
                var youtubeResult = MessageBox.Show(
                    this,
                    "Would you like to sign in to YouTube?\n\n" +
                    "Signing in helps avoid download restrictions and bot detection errors.\n\n" +
                    "You can always sign in later from Tools > Sign in to YouTube.",
                    "YouTube Sign In",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (youtubeResult == DialogResult.Yes)
                {
                    OpenYouTubeLogin();
                }
            }

            // Reset TopMost after first run dialogs
            this.TopMost = false;
        }

        await EnsureToolsAvailableAsync();

        // Start OneDrive monitoring if enabled
        if (config.MonitorOneDrive)
        {
            CheckOneDriveStatus(showSuccessMessage: false);
            StartOneDriveMonitoring();
        }

        // Initialize Channel Monitor
        InitializeChannelMonitor();

        // Update YouTube login status in status bar
        UpdateYouTubeStatus();

        // Check if version changed (after an update) and show changelog
        string currentVersion = AppUpdater.CurrentVersionString;
        if (!string.IsNullOrEmpty(config.LastKnownVersion) && config.LastKnownVersion != currentVersion)
        {
            logger.Log($"Version changed from {config.LastKnownVersion} to {currentVersion} - showing changelog");
            config.LastKnownVersion = currentVersion;
            config.Save();

            // Show changelog
            ChangelogMenuItem_Click(null, EventArgs.Empty);
        }
        else if (string.IsNullOrEmpty(config.LastKnownVersion))
        {
            // First time tracking version - just save current version
            config.LastKnownVersion = currentVersion;
            config.Save();
        }

        // Check for updates on startup (if enabled)
        await CheckForStartupUpdatesAsync();
    }

    private async Task CheckForStartupUpdatesAsync()
    {
        // Check for app updates
        if (config.AutoCheckAppUpdates && !string.IsNullOrEmpty(config.GitHubReleaseUrl))
        {
            try
            {
                var updateResult = await AppUpdater.CheckForUpdateAsync(config.GitHubReleaseUrl);

                if (updateResult.UpdateAvailable && !string.IsNullOrEmpty(updateResult.DownloadUrl))
                {
                    logger.Log($"App update available: {updateResult.LatestVersion} (current: {AppUpdater.CurrentVersionString})");

                    var result = MessageBox.Show(
                        this,
                        $"A new version of YouTube Downloader is available!\n\n" +
                        $"Current version: {AppUpdater.CurrentVersionString}\n" +
                        $"Latest version: {updateResult.LatestVersion}\n\n" +
                        "Would you like to download and install the update?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        // Trigger the manual update process
                        CheckForUpdatesMenuItem_Click(null, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Auto-update check failed: {ex.Message}");
            }
        }

        // Check for yt-dlp updates
        if (config.AutoCheckYtDlpUpdates && toolsManager.AreToolsAvailable())
        {
            try
            {
                string? currentVersion = await toolsManager.GetYtDlpVersionAsync();
                var (latestVersion, error) = await ToolsManager.GetLatestYtDlpVersionAsync();

                if (!string.IsNullOrEmpty(currentVersion) && !string.IsNullOrEmpty(latestVersion) && error == null)
                {
                    // Compare versions (yt-dlp uses dates like 2024.12.13)
                    if (string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        logger.Log($"yt-dlp update available: {latestVersion} (current: {currentVersion})");

                        var result = MessageBox.Show(
                            this,
                            $"A new version of yt-dlp is available!\n\n" +
                            $"Current version: {currentVersion}\n" +
                            $"Latest version: {latestVersion}\n\n" +
                            "Would you like to download the update?",
                            "yt-dlp Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            // Trigger the redownload
                            RedownloadYtDlpMenuItem_Click(null, EventArgs.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"yt-dlp update check failed: {ex.Message}");
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // If MinimizeOnClose is enabled and user clicked X, minimize instead of closing
        if (config.MinimizeOnClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            return;
        }

        logger.Log("Application closing");

        // Stop and dispose timers
        convertProgressTimer?.Stop();
        convertProgressTimer?.Dispose();
        jobTimer?.Stop();
        jobTimer?.Dispose();
        statsTimer?.Stop();
        statsTimer?.Dispose();

        // Dispose performance counters
        cpuCounter?.Dispose();
        availableMemCounter?.Dispose();

        // Stop OneDrive monitoring
        StopOneDriveMonitoring();

        // Stop Channel Monitor scan timer
        channelScanTimer?.Stop();
        channelScanTimer?.Dispose();

        // Clear temp folder on exit
        try
        {
            if (Directory.Exists(config.TempFolder))
            {
                var tempFiles = Directory.GetFiles(config.TempFolder, "*", SearchOption.AllDirectories);
                var tempDirs = Directory.GetDirectories(config.TempFolder);

                foreach (var file in tempFiles)
                {
                    try { File.Delete(file); } catch { }
                }
                foreach (var dir in tempDirs)
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
                logger.Log("Temp folder cleared");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Error clearing temp folder: {ex.Message}");
        }

        // Clean up temporary cookie file (security: don't leave decrypted cookies on disk)
        YouTubeCookieManager.DeleteCookieFile();

        logger.Dispose();
        base.OnFormClosing(e);
    }

    private async Task EnsureToolsAvailableAsync()
    {
        // Disable controls until tools are verified
        SetControlsEnabled(false);

        // Check if tools are available (await to satisfy async requirement)
        await Task.Yield();
        bool toolsReady = toolsManager.AreToolsAvailable();

        if (!toolsReady)
        {
            // Determine which tools are missing
            bool ytDlpMissing = string.IsNullOrEmpty(toolsManager.YtDlpPath) || !File.Exists(toolsManager.YtDlpPath);
            bool ffmpegMissing = string.IsNullOrEmpty(toolsManager.FfmpegPath) ||
                                !File.Exists(Path.Combine(toolsManager.FfmpegPath, "ffmpeg.exe"));

            string missingTools = "";
            if (ytDlpMissing && ffmpegMissing)
                missingTools = "yt-dlp and ffmpeg are";
            else if (ytDlpMissing)
                missingTools = "yt-dlp is";
            else if (ffmpegMissing)
                missingTools = "ffmpeg is";

            var result = MessageBox.Show(
                $"{missingTools} missing.\n\n" +
                $"Would you like to download the required tools now?\n\n" +
                $"Note: Download buttons will be disabled until the tools are available.\n" +
                $"You can download them later from Tools menu.",
                "Missing Tools",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Show download dialog popup
                using var downloadForm = new ToolsDownloadForm(toolsManager);
                if (downloadForm.ShowDialog(this) != DialogResult.OK)
                {
                    ShowToolsMissingStatus();
                    return;
                }
            }
            else
            {
                ShowToolsMissingStatus();
                return;
            }
        }

        // Final validation - ensure tools are actually available
        if (!toolsManager.AreToolsAvailable())
        {
            ShowToolsMissingStatus();
            return;
        }

        // Update paths from tools manager
        ytDlpPath = toolsManager.YtDlpPath;
        ffmpegPath = toolsManager.FfmpegPath;

        logger.Log($"yt-dlp path: {ytDlpPath}");
        logger.Log($"ffmpeg path: {ffmpegPath}");

        // Check for Deno (required for YouTube signature solving)
        if (!toolsManager.IsDenoAvailable())
        {
            logger.Log("Deno runtime not found");
            var denoResult = MessageBox.Show(
                "Deno runtime is missing.\n\n" +
                "Deno is required for yt-dlp to handle YouTube's signature protection.\n" +
                "Without it, downloads may fail.\n\n" +
                "Would you like to download Deno now?",
                "Deno Runtime Missing",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (denoResult == DialogResult.Yes)
            {
                using var downloadForm = new DownloadProgressForm("Downloading Deno Runtime");
                var downloadTask = downloadForm.RunDownloadAsync(async (progress, token) =>
                {
                    return await toolsManager.RedownloadDenoAsync(progress);
                });

                downloadForm.ShowDialog(this);
                await downloadTask;

                if (downloadForm.WasSuccessful)
                {
                    logger.Log($"Deno downloaded to: {toolsManager.DenoPath}");
                }
                else if (!downloadForm.WasCancelled)
                {
                    logger.Log("Deno download failed");
                    MessageBox.Show(
                        "Failed to download Deno runtime.\n\n" +
                        "You can try again later from Tools > Redownload Deno.",
                        "Download Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                logger.Log("User declined Deno download");
            }
        }
        else
        {
            logger.Log($"Deno path: {toolsManager.DenoPath}");
        }

        statusLabel.Text = "Ready";
        SetControlsEnabled(true);
    }

    private void ShowToolsMissingStatus()
    {
        bool ytDlpMissing = string.IsNullOrEmpty(toolsManager.YtDlpPath) || !File.Exists(toolsManager.YtDlpPath);
        bool ffmpegMissing = string.IsNullOrEmpty(toolsManager.FfmpegPath) ||
                            !File.Exists(Path.Combine(toolsManager.FfmpegPath, "ffmpeg.exe"));

        string missingList = "";
        if (ytDlpMissing) missingList += "yt-dlp ";
        if (ffmpegMissing) missingList += "ffmpeg ";

        statusLabel.Text = $"Tools missing: {missingList.Trim()}. Use Tools menu to download.";
        logger.Log($"Tools missing - yt-dlp: {(ytDlpMissing ? "Missing" : "OK")}, ffmpeg: {(ffmpegMissing ? "Missing" : "OK")}");

        // Keep Go buttons disabled, but enable other UI elements
        goButton.Enabled = false;
        playlistGoButton.Enabled = false;
        playlistBrowseButton.Enabled = false;

        // Enable text boxes and other controls so user can still configure
        urlTextBox.Enabled = true;
        playlistUrlTextBox.Enabled = true;
        singleFolderComboBox.Enabled = true;
        playlistFolderComboBox.Enabled = true;
    }
}
