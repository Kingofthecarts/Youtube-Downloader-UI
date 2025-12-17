namespace Youtube_Downloader;

public class Logger : IDisposable
{
    private readonly string logDir;
    private readonly string logPath;
    private readonly StreamWriter writer;
    private bool disposed = false;

    public string LogFilePath => logPath;
    public string LogDirectory => logDir;

    public Logger(string? logFolder = null)
    {
        // Use provided log folder or default to AppData
        if (string.IsNullOrEmpty(logFolder))
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YoutubeDownloader");
            logDir = Path.Combine(appDataDir, "logs");
        }
        else
        {
            logDir = logFolder;
        }
        Directory.CreateDirectory(logDir);

        // Determine session number for today
        string datePrefix = DateTime.Now.ToString("yyyy-MM-dd");
        int sessionNumber = GetNextSessionNumber(datePrefix);

        string fileName = $"{datePrefix}-session{sessionNumber}.txt";
        logPath = Path.Combine(logDir, fileName);

        writer = new StreamWriter(logPath, append: false) { AutoFlush = true };

        // Write header
        WriteHeader();

        // Auto-clear old logs if there are 100 or more
        AutoClearOldLogsIfNeeded();
    }

    private void AutoClearOldLogsIfNeeded()
    {
        try
        {
            int logCount = Directory.GetFiles(logDir, "*.txt").Length;
            if (logCount >= 100)
            {
                Log($"Auto-clearing old logs (found {logCount} log files)");
                ClearOldLogs(20);
            }
        }
        catch
        {
            // Ignore errors during auto-cleanup
        }
    }

    private int GetNextSessionNumber(string datePrefix)
    {
        var existingLogs = Directory.GetFiles(logDir, $"{datePrefix}-session*.txt");

        int maxSession = 0;
        foreach (var file in existingLogs)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            // Extract session number from filename like "2025-12-06-session3"
            int sessionIndex = fileName.LastIndexOf("session");
            if (sessionIndex >= 0)
            {
                string sessionPart = fileName.Substring(sessionIndex + 7);
                if (int.TryParse(sessionPart, out int num))
                {
                    maxSession = Math.Max(maxSession, num);
                }
            }
        }

        return maxSession + 1;
    }

    private void WriteHeader()
    {
        writer.WriteLine("═══════════════════════════════════════════════════════════════");
        writer.WriteLine($"  YouTube Downloader - Session Log");
        writer.WriteLine($"  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine("═══════════════════════════════════════════════════════════════");
        writer.WriteLine();
    }

    public void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        writer.WriteLine($"[{timestamp}] {message}");
    }

    public void LogSection(string title)
    {
        writer.WriteLine();
        writer.WriteLine($"─── {title} ───");
    }

    public void LogDownloadStart(string url, bool isPlaylist, string? playlistFolder)
    {
        LogSection(isPlaylist ? "PLAYLIST DOWNLOAD" : "SINGLE VIDEO DOWNLOAD");
        Log($"URL: {url}");
        if (isPlaylist && !string.IsNullOrEmpty(playlistFolder))
        {
            Log($"Playlist Folder: {playlistFolder}");
        }
    }

    public void LogDownloadProgress(string message)
    {
        // For progress updates, just log the raw message
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        writer.WriteLine($"[{timestamp}] {message}");
    }

    public void LogYtDlpOutput(string? data)
    {
        if (string.IsNullOrEmpty(data)) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        writer.WriteLine($"[{timestamp}] [yt-dlp] {data}");
    }

    public void LogDownloadComplete(string? filePath, long? fileSize)
    {
        Log("Download completed successfully");
        if (!string.IsNullOrEmpty(filePath))
        {
            Log($"Output: {filePath}");
        }
        if (fileSize.HasValue)
        {
            Log($"Size: {FormatFileSize(fileSize.Value)}");
        }
    }

    public void LogPlaylistComplete(int totalItems, string folderPath)
    {
        Log($"Playlist completed: {totalItems} items");
        Log($"Output folder: {folderPath}");
    }

    public void LogError(string error)
    {
        Log($"ERROR: {error}");
    }

    public void LogHistoryAdded(string title, string videoId)
    {
        Log($"Added to history: {title} [{videoId}]");
    }

    public void LogConfigChange(string setting, string value)
    {
        Log($"Config changed: {setting} = {value}");
    }

    public void ClearOldLogs(int keepCount = 20)
    {
        try
        {
            var logFiles = Directory.GetFiles(logDir, "*.txt")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            // Skip the current log file and keep only the specified number
            var filesToDelete = logFiles
                .Where(f => f.FullName != logPath)
                .Skip(keepCount - 1) // -1 because current log counts toward the limit
                .ToList();

            foreach (var file in filesToDelete)
            {
                try
                {
                    file.Delete();
                    Log($"Deleted old log: {file.Name}");
                }
                catch
                {
                    // Ignore individual file deletion errors
                }
            }

            if (filesToDelete.Count > 0)
            {
                Log($"Cleared {filesToDelete.Count} old log file(s)");
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    public int GetLogFileCount()
    {
        try
        {
            return Directory.GetFiles(logDir, "*.txt").Length;
        }
        catch
        {
            return 0;
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public void Dispose()
    {
        if (!disposed)
        {
            writer.WriteLine();
            writer.WriteLine("═══════════════════════════════════════════════════════════════");
            writer.WriteLine($"  Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine("═══════════════════════════════════════════════════════════════");
            writer.Flush();
            writer.Dispose();
            disposed = true;
        }
    }
}
