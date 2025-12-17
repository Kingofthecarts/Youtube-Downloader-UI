using System.Xml.Linq;

namespace Youtube_Downloader;

/// <summary>
/// Represents download statistics for a single day
/// </summary>
public class DailyStats
{
    public DateTime Date { get; set; }
    public int SongCount { get; set; }
    public long TotalBytes { get; set; }

    public string TotalSizeFormatted
    {
        get
        {
            if (TotalBytes < 1024) return $"{TotalBytes} B";
            if (TotalBytes < 1024 * 1024) return $"{TotalBytes / 1024.0:F1} KB";
            if (TotalBytes < 1024L * 1024 * 1024) return $"{TotalBytes / (1024.0 * 1024.0):F2} MB";
            return $"{TotalBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}

/// <summary>
/// Tracks download statistics per day
/// </summary>
public class DownloadStats
{
    private readonly string statsPath;
    private Dictionary<DateTime, DailyStats> dailyStats = new();

    public DownloadStats()
    {
        string appDir = AppPaths.AppDirectory;
        statsPath = Path.Combine(appDir, "stats.xml");
        Load();
    }

    public void Load()
    {
        dailyStats.Clear();

        if (!File.Exists(statsPath))
        {
            return;
        }

        try
        {
            var doc = XDocument.Load(statsPath);
            var root = doc.Element("DownloadStats");

            if (root != null)
            {
                foreach (var dayElement in root.Elements("Day"))
                {
                    var dateStr = dayElement.Element("Date")?.Value;
                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        var stats = new DailyStats
                        {
                            Date = date.Date,
                            SongCount = int.TryParse(dayElement.Element("SongCount")?.Value, out var count) ? count : 0,
                            TotalBytes = long.TryParse(dayElement.Element("TotalBytes")?.Value, out var bytes) ? bytes : 0
                        };
                        dailyStats[date.Date] = stats;
                    }
                }
            }
        }
        catch
        {
            // If file is corrupted, start fresh
            dailyStats.Clear();
        }
    }

    public void Save()
    {
        var doc = new XDocument(
            new XElement("DownloadStats",
                dailyStats.Values
                    .OrderByDescending(s => s.Date)
                    .Select(s => new XElement("Day",
                        new XElement("Date", s.Date.ToString("yyyy-MM-dd")),
                        new XElement("SongCount", s.SongCount),
                        new XElement("TotalBytes", s.TotalBytes)
                    ))
            )
        );

        doc.Save(statsPath);
    }

    /// <summary>
    /// Record a download for today
    /// </summary>
    public void RecordDownload(long fileSizeBytes)
    {
        var today = DateTime.Today;

        if (!dailyStats.TryGetValue(today, out var stats))
        {
            stats = new DailyStats { Date = today };
            dailyStats[today] = stats;
        }

        stats.SongCount++;
        stats.TotalBytes += fileSizeBytes;
        Save();
    }

    /// <summary>
    /// Get all daily stats ordered by date descending
    /// </summary>
    public List<DailyStats> GetAllStats()
    {
        return dailyStats.Values
            .OrderByDescending(s => s.Date)
            .ToList();
    }

    /// <summary>
    /// Get stats for today
    /// </summary>
    public DailyStats? GetTodayStats()
    {
        return dailyStats.TryGetValue(DateTime.Today, out var stats) ? stats : null;
    }

    /// <summary>
    /// Get total songs downloaded all time
    /// </summary>
    public int GetTotalSongCount()
    {
        return dailyStats.Values.Sum(s => s.SongCount);
    }

    /// <summary>
    /// Get total bytes downloaded all time
    /// </summary>
    public long GetTotalBytes()
    {
        return dailyStats.Values.Sum(s => s.TotalBytes);
    }

    /// <summary>
    /// Get formatted total size
    /// </summary>
    public string GetTotalSizeFormatted()
    {
        long total = GetTotalBytes();
        if (total < 1024) return $"{total} B";
        if (total < 1024 * 1024) return $"{total / 1024.0:F1} KB";
        if (total < 1024L * 1024 * 1024) return $"{total / (1024.0 * 1024.0):F2} MB";
        return $"{total / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    /// <summary>
    /// Get stats for the last N days
    /// </summary>
    public List<DailyStats> GetRecentStats(int days)
    {
        var cutoff = DateTime.Today.AddDays(-days + 1);
        return dailyStats.Values
            .Where(s => s.Date >= cutoff)
            .OrderByDescending(s => s.Date)
            .ToList();
    }

    /// <summary>
    /// Get the number of days with downloads
    /// </summary>
    public int GetDaysWithDownloads()
    {
        return dailyStats.Count;
    }
}
