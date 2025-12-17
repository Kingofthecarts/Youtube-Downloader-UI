using System.Xml.Linq;

namespace Youtube_Downloader;

public class Config
{
    private const string ConfigFileName = "config.xml";

    private readonly string appSideConfigPath;
    private readonly string appDataConfigPath;
    private readonly string defaultTempFolder;

    // The active config path (where we load from and save to)
    private string configPath;

    // Default download URLs
    public const string DefaultYtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    public const string DefaultFfmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";
    public const string DefaultDenoUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";
    public const string DefaultWebView2Url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
    public const string DefaultGitHubReleaseUrl = "https://github.com/Kingofthecarts/Youtube-Downloader-UI";

    public string OutputFolder { get; set; }
    public string TempFolder { get; set; }
    public string FfmpegFolderName { get; set; }
    public string YtDlpPath { get; set; }
    public string FfmpegPath { get; set; }
    public bool IsFirstRun { get; private set; }
    public int DownloadDelaySeconds { get; set; }
    public string YtDlpDownloadUrl { get; set; }
    public string FfmpegDownloadUrl { get; set; }
    public string DenoPath { get; set; }
    public string DenoDownloadUrl { get; set; }
    public string WebView2DownloadUrl { get; set; }

    // YouTube authentication
    public string YouTubeCookiesEncrypted { get; set; }
    public bool YouTubeLoggedIn { get; set; }
    public string YouTubeLoginEmail { get; set; }

    // Song delete feature settings
    public bool AllowSongDelete { get; set; }
    public bool SongDeleteRequireCountdown { get; set; }
    public int SongDeleteCountdownSeconds { get; set; }

    // Download options
    public bool EmbedThumbnail { get; set; }

    // Folder history options
    public bool SortFoldersByRecent { get; set; }

    // OneDrive monitoring
    public bool MonitorOneDrive { get; set; }

    // Channel Monitor
    public int ChannelScanIntervalMinutes { get; set; }
    public int MaxChannelVideosShown { get; set; }  // 0 = unlimited
    public bool ChannelAutoScanEnabled { get; set; }

    // Performance tracking
    public bool EnablePerformanceTracking { get; set; }
    public int LastSongBrowserOpenMs { get; set; }
    public int LastHistoryOpenMs { get; set; }
    public int LastSongLoadMs { get; set; }

    // Auto-update
    public string GitHubReleaseUrl { get; set; }

    // Window behavior
    public bool MinimizeOnClose { get; set; }

    // Edit form settings
    public bool AllowFilenameEdit { get; set; }

    // Log folder location
    public string LogFolder { get; set; }

    // Folder history file path (portable - beside app)
    public string FolderHistoryPath { get; set; }

    /// <summary>
    /// Returns true if config is stored beside the app, false if in AppData
    /// </summary>
    public bool IsStoredBesideApp => string.Equals(
        Path.GetFullPath(configPath),
        Path.GetFullPath(appSideConfigPath),
        StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the current config file location
    /// </summary>
    public string ConfigLocation => configPath;

    /// <summary>
    /// Gets the AppData config path (for display purposes)
    /// </summary>
    public string AppDataConfigPath => appDataConfigPath;

    /// <summary>
    /// Gets the app-side config path (for display purposes)
    /// </summary>
    public string AppSideConfigPath => appSideConfigPath;

    /// <summary>
    /// Resolves the folder history path - converts relative paths to absolute based on app directory
    /// </summary>
    public string ResolvedFolderHistoryPath
    {
        get
        {
            if (string.IsNullOrEmpty(FolderHistoryPath))
                return Path.Combine(AppPaths.AppDirectory, "folder_history.xml");

            // Check if it's a relative path (starts with .\ or ./)
            if (FolderHistoryPath.StartsWith(@".\") || FolderHistoryPath.StartsWith("./"))
            {
                return Path.Combine(AppPaths.AppDirectory, FolderHistoryPath.Substring(2));
            }

            // If it's already absolute, return as-is
            if (Path.IsPathRooted(FolderHistoryPath))
                return FolderHistoryPath;

            // Otherwise treat as relative to app directory
            return Path.Combine(AppPaths.AppDirectory, FolderHistoryPath);
        }
    }

    public Config()
    {
        string appDir = AppPaths.AppDirectory;
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeDownloader");

        appSideConfigPath = Path.Combine(appDir, ConfigFileName);
        appDataConfigPath = Path.Combine(appDataDir, ConfigFileName);
        defaultTempFolder = Path.Combine(appDataDir, "temp");

        // Determine which config to use: app-side takes priority, then AppData, then new in AppData
        if (File.Exists(appSideConfigPath))
        {
            configPath = appSideConfigPath;
        }
        else if (File.Exists(appDataConfigPath))
        {
            configPath = appDataConfigPath;
        }
        else
        {
            // New install - default to AppData
            configPath = appDataConfigPath;
        }

        // Default to empty - will prompt user on first run
        OutputFolder = "";
        TempFolder = defaultTempFolder;
        FfmpegFolderName = "";
        YtDlpPath = "";
        FfmpegPath = "";
        IsFirstRun = false;
        DownloadDelaySeconds = 10;
        YtDlpDownloadUrl = DefaultYtDlpUrl;
        FfmpegDownloadUrl = DefaultFfmpegUrl;
        DenoPath = "";
        DenoDownloadUrl = DefaultDenoUrl;
        WebView2DownloadUrl = DefaultWebView2Url;
        YouTubeCookiesEncrypted = "";
        YouTubeLoggedIn = false;
        YouTubeLoginEmail = "";
        AllowSongDelete = false;
        SongDeleteRequireCountdown = true;
        SongDeleteCountdownSeconds = 5;
        EmbedThumbnail = true;
        SortFoldersByRecent = false;
        MonitorOneDrive = false;
        ChannelScanIntervalMinutes = 60;
        MaxChannelVideosShown = 0;  // 0 = unlimited
        ChannelAutoScanEnabled = true;
        EnablePerformanceTracking = false;
        LastSongBrowserOpenMs = 0;
        LastHistoryOpenMs = 0;
        LastSongLoadMs = 0;
        GitHubReleaseUrl = DefaultGitHubReleaseUrl;
        MinimizeOnClose = false;
        AllowFilenameEdit = false;
        LogFolder = Path.Combine(appDataDir, "logs");
        FolderHistoryPath = @".\folder_history.xml";  // Relative = beside app (portable)

        Load();
    }

    public void Load()
    {
        if (!File.Exists(configPath))
        {
            IsFirstRun = true;
            return; // Don't save yet - wait for user to set output folder
        }

        try
        {
            var doc = XDocument.Load(configPath);
            var root = doc.Element("Config");

            if (root != null)
            {
                var outputFolder = root.Element("OutputFolder")?.Value;
                if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder))
                {
                    OutputFolder = outputFolder;
                }

                var tempFolder = root.Element("TempFolder")?.Value;
                if (!string.IsNullOrEmpty(tempFolder))
                {
                    TempFolder = tempFolder;
                }

                var ffmpegFolder = root.Element("FfmpegFolderName")?.Value;
                if (!string.IsNullOrEmpty(ffmpegFolder))
                {
                    FfmpegFolderName = ffmpegFolder;
                }

                var ytDlpPath = root.Element("YtDlpPath")?.Value;
                if (!string.IsNullOrEmpty(ytDlpPath))
                {
                    YtDlpPath = ytDlpPath;
                }

                var ffmpegPath = root.Element("FfmpegPath")?.Value;
                if (!string.IsNullOrEmpty(ffmpegPath))
                {
                    FfmpegPath = ffmpegPath;
                }

                var delaySeconds = root.Element("DownloadDelaySeconds")?.Value;
                if (int.TryParse(delaySeconds, out var delay) && delay >= 0)
                {
                    DownloadDelaySeconds = Math.Min(delay, 3599); // Max 59:59
                }

                var ytDlpUrl = root.Element("YtDlpDownloadUrl")?.Value;
                if (!string.IsNullOrEmpty(ytDlpUrl))
                {
                    YtDlpDownloadUrl = ytDlpUrl;
                }

                var ffmpegUrl = root.Element("FfmpegDownloadUrl")?.Value;
                if (!string.IsNullOrEmpty(ffmpegUrl))
                {
                    FfmpegDownloadUrl = ffmpegUrl;
                }

                var denoPath = root.Element("DenoPath")?.Value;
                if (!string.IsNullOrEmpty(denoPath))
                {
                    DenoPath = denoPath;
                }

                var denoUrl = root.Element("DenoDownloadUrl")?.Value;
                if (!string.IsNullOrEmpty(denoUrl))
                {
                    DenoDownloadUrl = denoUrl;
                }

                var webView2Url = root.Element("WebView2DownloadUrl")?.Value;
                if (!string.IsNullOrEmpty(webView2Url))
                {
                    WebView2DownloadUrl = webView2Url;
                }

                var youtubeCookies = root.Element("YouTubeCookiesEncrypted")?.Value;
                if (!string.IsNullOrEmpty(youtubeCookies))
                {
                    YouTubeCookiesEncrypted = youtubeCookies;
                }

                var youtubeLoggedIn = root.Element("YouTubeLoggedIn")?.Value;
                if (bool.TryParse(youtubeLoggedIn, out var loggedIn))
                {
                    YouTubeLoggedIn = loggedIn;
                }

                var youtubeEmail = root.Element("YouTubeLoginEmail")?.Value;
                if (!string.IsNullOrEmpty(youtubeEmail))
                {
                    YouTubeLoginEmail = youtubeEmail;
                }

                var allowSongDelete = root.Element("AllowSongDelete")?.Value;
                if (bool.TryParse(allowSongDelete, out var allowDelete))
                {
                    AllowSongDelete = allowDelete;
                }

                var songDeleteRequireCountdown = root.Element("SongDeleteRequireCountdown")?.Value;
                if (bool.TryParse(songDeleteRequireCountdown, out var requireCountdown))
                {
                    SongDeleteRequireCountdown = requireCountdown;
                }

                var songDeleteCountdown = root.Element("SongDeleteCountdownSeconds")?.Value;
                if (int.TryParse(songDeleteCountdown, out var countdown) && countdown >= 0)
                {
                    SongDeleteCountdownSeconds = Math.Min(countdown, 30); // Max 30 seconds
                }

                var embedThumbnail = root.Element("EmbedThumbnail")?.Value;
                if (bool.TryParse(embedThumbnail, out var embed))
                {
                    EmbedThumbnail = embed;
                }

                var sortFoldersByRecent = root.Element("SortFoldersByRecent")?.Value;
                if (bool.TryParse(sortFoldersByRecent, out var sortRecent))
                {
                    SortFoldersByRecent = sortRecent;
                }

                var monitorOneDrive = root.Element("MonitorOneDrive")?.Value;
                if (bool.TryParse(monitorOneDrive, out var monitor))
                {
                    MonitorOneDrive = monitor;
                }

                var channelScanInterval = root.Element("ChannelScanIntervalMinutes")?.Value;
                if (int.TryParse(channelScanInterval, out var scanInterval) && scanInterval >= 1 && scanInterval <= 60)
                {
                    ChannelScanIntervalMinutes = scanInterval;
                }

                var maxChannelVideos = root.Element("MaxChannelVideosShown")?.Value;
                if (int.TryParse(maxChannelVideos, out var maxVids) && maxVids >= 0 && maxVids <= 500)
                {
                    MaxChannelVideosShown = maxVids;
                }

                var channelAutoScan = root.Element("ChannelAutoScanEnabled")?.Value;
                if (bool.TryParse(channelAutoScan, out var autoScan))
                {
                    ChannelAutoScanEnabled = autoScan;
                }

                var enablePerfTracking = root.Element("EnablePerformanceTracking")?.Value;
                if (bool.TryParse(enablePerfTracking, out var perfTracking))
                {
                    EnablePerformanceTracking = perfTracking;
                }

                var lastSongBrowserOpen = root.Element("LastSongBrowserOpenMs")?.Value;
                if (int.TryParse(lastSongBrowserOpen, out var songBrowserMs) && songBrowserMs >= 0)
                {
                    LastSongBrowserOpenMs = songBrowserMs;
                }

                var lastHistoryOpen = root.Element("LastHistoryOpenMs")?.Value;
                if (int.TryParse(lastHistoryOpen, out var historyMs) && historyMs >= 0)
                {
                    LastHistoryOpenMs = historyMs;
                }

                var lastSongLoad = root.Element("LastSongLoadMs")?.Value;
                if (int.TryParse(lastSongLoad, out var songLoadMs) && songLoadMs >= 0)
                {
                    LastSongLoadMs = songLoadMs;
                }

                var gitHubReleaseUrl = root.Element("GitHubReleaseUrl")?.Value;
                if (!string.IsNullOrEmpty(gitHubReleaseUrl))
                {
                    GitHubReleaseUrl = gitHubReleaseUrl;
                }

                var minimizeOnClose = root.Element("MinimizeOnClose")?.Value;
                if (bool.TryParse(minimizeOnClose, out var minOnClose))
                {
                    MinimizeOnClose = minOnClose;
                }

                var allowFilenameEdit = root.Element("AllowFilenameEdit")?.Value;
                if (bool.TryParse(allowFilenameEdit, out var allowEdit))
                {
                    AllowFilenameEdit = allowEdit;
                }

                var logFolder = root.Element("LogFolder")?.Value;
                if (!string.IsNullOrEmpty(logFolder))
                {
                    LogFolder = logFolder;
                }

                var folderHistoryPath = root.Element("FolderHistoryPath")?.Value;
                if (!string.IsNullOrEmpty(folderHistoryPath))
                {
                    FolderHistoryPath = folderHistoryPath;
                }
            }
        }
        catch
        {
            // If config is corrupted, use defaults and recreate
            Save();
        }
    }

    public void Save()
    {
        // Ensure directory exists (especially for AppData)
        string? dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var doc = new XDocument(
            new XElement("Config",
                new XElement("OutputFolder", OutputFolder),
                new XElement("TempFolder", TempFolder),
                new XElement("FfmpegFolderName", FfmpegFolderName),
                new XElement("YtDlpPath", YtDlpPath),
                new XElement("FfmpegPath", FfmpegPath),
                new XElement("DownloadDelaySeconds", DownloadDelaySeconds),
                new XElement("YtDlpDownloadUrl", YtDlpDownloadUrl),
                new XElement("FfmpegDownloadUrl", FfmpegDownloadUrl),
                new XElement("DenoPath", DenoPath),
                new XElement("DenoDownloadUrl", DenoDownloadUrl),
                new XElement("WebView2DownloadUrl", WebView2DownloadUrl),
                new XElement("YouTubeCookiesEncrypted", YouTubeCookiesEncrypted),
                new XElement("YouTubeLoggedIn", YouTubeLoggedIn),
                new XElement("YouTubeLoginEmail", YouTubeLoginEmail),
                new XElement("AllowSongDelete", AllowSongDelete),
                new XElement("SongDeleteRequireCountdown", SongDeleteRequireCountdown),
                new XElement("SongDeleteCountdownSeconds", SongDeleteCountdownSeconds),
                new XElement("EmbedThumbnail", EmbedThumbnail),
                new XElement("SortFoldersByRecent", SortFoldersByRecent),
                new XElement("MonitorOneDrive", MonitorOneDrive),
                new XElement("ChannelScanIntervalMinutes", ChannelScanIntervalMinutes),
                new XElement("MaxChannelVideosShown", MaxChannelVideosShown),
                new XElement("ChannelAutoScanEnabled", ChannelAutoScanEnabled),
                new XElement("EnablePerformanceTracking", EnablePerformanceTracking),
                new XElement("LastSongBrowserOpenMs", LastSongBrowserOpenMs),
                new XElement("LastHistoryOpenMs", LastHistoryOpenMs),
                new XElement("LastSongLoadMs", LastSongLoadMs),
                new XElement("GitHubReleaseUrl", GitHubReleaseUrl),
                new XElement("MinimizeOnClose", MinimizeOnClose),
                new XElement("AllowFilenameEdit", AllowFilenameEdit),
                new XElement("LogFolder", LogFolder),
                new XElement("FolderHistoryPath", FolderHistoryPath)
            )
        );

        doc.Save(configPath);
    }

    /// <summary>
    /// Moves the config file to the other location (AppData <-> beside app)
    /// </summary>
    /// <returns>True if move was successful</returns>
    public bool MoveConfigLocation()
    {
        string oldPath = configPath;
        string newPath = IsStoredBesideApp ? appDataConfigPath : appSideConfigPath;

        try
        {
            // Ensure target directory exists
            string? dir = Path.GetDirectoryName(newPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Save to new location
            configPath = newPath;
            Save();

            // Delete old file
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }

            return true;
        }
        catch
        {
            // Revert to old path on failure
            configPath = oldPath;
            return false;
        }
    }
}
