using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youtube_Downloader;

public class PlaylistTrack
{
    public string VideoId { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Folder { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public bool IsExcluded { get; set; } = false;
    public int PlaylistIndex { get; set; } = 0;
    public string ChannelId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public double DownloadTimeSeconds { get; set; }
    public double ConvertTimeSeconds { get; set; }
    public DateTime DownloadDate { get; set; }

    [JsonIgnore]
    public string ChannelUrl => !string.IsNullOrEmpty(ChannelId)
        ? $"https://www.youtube.com/channel/{ChannelId}"
        : "";

    [JsonIgnore]
    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }
}

public class DownloadRecord
{
    public string VideoId { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime DownloadDate { get; set; }
    public long FileSizeBytes { get; set; }
    public string FilePath { get; set; } = "";
    public string DownloadFolder { get; set; } = "";
    public bool IsPlaylist { get; set; } = false;
    public int PlaylistItemCount { get; set; } = 0;
    public List<PlaylistTrack> PlaylistTracks { get; set; } = new();
    public bool IsSuperseded { get; set; } = false;
    public string ChannelId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public double DownloadTimeSeconds { get; set; }
    public double ConvertTimeSeconds { get; set; }

    [JsonIgnore]
    public string ChannelUrl => !string.IsNullOrEmpty(ChannelId)
        ? $"https://www.youtube.com/channel/{ChannelId}"
        : "";

    [JsonIgnore]
    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }
}

public class DownloadHistory
{
    private readonly string singlesHistoryPath;
    private readonly string playlistsHistoryPath;
    private List<DownloadRecord> singlesRecords = new();
    private List<DownloadRecord> playlistsRecords = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public IReadOnlyList<DownloadRecord> SingleRecords => singlesRecords.AsReadOnly();
    public IReadOnlyList<DownloadRecord> PlaylistRecords => playlistsRecords.AsReadOnly();

    public string SinglesHistoryPath => singlesHistoryPath;
    public string PlaylistsHistoryPath => playlistsHistoryPath;

    public DownloadHistory()
    {
        string appDir = AppPaths.AppDirectory;
        singlesHistoryPath = Path.Combine(appDir, "history_singles.json");
        playlistsHistoryPath = Path.Combine(appDir, "history_playlists.json");
        EnsureFilesExist();
        Load();
    }

    public void EnsureFilesExist()
    {
        // Create empty history files if they don't exist
        if (!File.Exists(singlesHistoryPath))
        {
            File.WriteAllText(singlesHistoryPath, "[]");
        }
        if (!File.Exists(playlistsHistoryPath))
        {
            File.WriteAllText(playlistsHistoryPath, "[]");
        }
    }

    public void Load()
    {
        singlesRecords.Clear();
        playlistsRecords.Clear();

        // Load singles history
        singlesRecords = LoadFromFile(singlesHistoryPath);

        // Load playlists history
        playlistsRecords = LoadFromFile(playlistsHistoryPath);
    }

    private List<DownloadRecord> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<DownloadRecord>();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return new List<DownloadRecord>();
            }

            return JsonSerializer.Deserialize<List<DownloadRecord>>(json, JsonOptions)
                   ?? new List<DownloadRecord>();
        }
        catch
        {
            // Silently fail - file may be corrupted
            return new List<DownloadRecord>();
        }
    }

    public void Save()
    {
        SaveToFile(singlesHistoryPath, singlesRecords);
        SaveToFile(playlistsHistoryPath, playlistsRecords);
    }

    private void SaveToFile(string filePath, List<DownloadRecord> records)
    {
        try
        {
            string json = JsonSerializer.Serialize(records, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    public void MarkAsSuperseded(string videoId)
    {
        var record = FindByVideoId(videoId);
        if (record != null && !record.IsSuperseded)
        {
            record.IsSuperseded = true;
            SaveToFile(singlesHistoryPath, singlesRecords);
        }
    }

    public void MarkPlaylistAsSuperseded(string playlistId)
    {
        var record = FindByPlaylistId(playlistId);
        if (record != null && !record.IsSuperseded)
        {
            record.IsSuperseded = true;
            SaveToFile(playlistsHistoryPath, playlistsRecords);
        }
    }

    public void AddRecord(DownloadRecord record)
    {
        if (record.IsPlaylist)
        {
            playlistsRecords.Insert(0, record);
            SaveToFile(playlistsHistoryPath, playlistsRecords);
        }
        else
        {
            singlesRecords.Insert(0, record);
            SaveToFile(singlesHistoryPath, singlesRecords);
        }
    }

    public DownloadRecord? FindByVideoId(string videoId)
    {
        if (string.IsNullOrEmpty(videoId)) return null;
        return singlesRecords.FirstOrDefault(r => r.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));
    }

    public DownloadRecord? FindByUrl(string url)
    {
        string? videoId = ExtractVideoId(url);
        if (videoId != null)
        {
            return FindByVideoId(videoId);
        }
        return singlesRecords.FirstOrDefault(r => r.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
    }

    public DownloadRecord? FindByPlaylistId(string playlistId)
    {
        if (string.IsNullOrEmpty(playlistId)) return null;
        return playlistsRecords.FirstOrDefault(r => r.VideoId.Equals(playlistId, StringComparison.OrdinalIgnoreCase));
    }

    public static string? ExtractVideoId(string url)
    {
        try
        {
            var uri = new Uri(url);

            if (uri.Host.Contains("youtu.be"))
            {
                return uri.AbsolutePath.TrimStart('/').Split('?')[0];
            }

            if (uri.Host.Contains("youtube.com"))
            {
                var queryString = uri.Query.TrimStart('?');
                var queryParams = queryString.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0], p => p[1]);

                if (queryParams.TryGetValue("v", out var videoId) && !string.IsNullOrEmpty(videoId))
                {
                    return videoId;
                }

                var pathParts = uri.AbsolutePath.Split('/');
                if (pathParts.Length >= 3 && (pathParts[1] == "embed" || pathParts[1] == "v"))
                {
                    return pathParts[2].Split('?')[0];
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public static string? ExtractPlaylistId(string url)
    {
        try
        {
            var uri = new Uri(url);

            if (uri.Host.Contains("youtube.com"))
            {
                var queryString = uri.Query.TrimStart('?');
                var queryParams = queryString.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0], p => p[1]);

                if (queryParams.TryGetValue("list", out var listId) && !string.IsNullOrEmpty(listId))
                {
                    return listId;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public void DeleteRecord(DownloadRecord record)
    {
        if (record.IsPlaylist)
        {
            playlistsRecords.Remove(record);
            SaveToFile(playlistsHistoryPath, playlistsRecords);
        }
        else
        {
            singlesRecords.Remove(record);
            SaveToFile(singlesHistoryPath, singlesRecords);
        }
    }

    public void ClearAll()
    {
        singlesRecords.Clear();
        playlistsRecords.Clear();
        Save();
    }

    public void ClearSingles()
    {
        singlesRecords.Clear();
        SaveToFile(singlesHistoryPath, singlesRecords);
    }

    public void ClearPlaylists()
    {
        playlistsRecords.Clear();
        SaveToFile(playlistsHistoryPath, playlistsRecords);
    }

    /// <summary>
    /// Check if a video ID exists in any history record (singles or playlist tracks)
    /// </summary>
    /// <param name="videoId">The video ID to search for</param>
    /// <param name="downloadDate">The date when the video was downloaded (if found)</param>
    /// <returns>True if the video ID was found in history</returns>
    public bool HasVideoId(string videoId, out string downloadDate)
    {
        downloadDate = "";
        if (string.IsNullOrEmpty(videoId)) return false;

        // Check singles history
        var singleRecord = singlesRecords.FirstOrDefault(r =>
            r.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));
        if (singleRecord != null)
        {
            downloadDate = singleRecord.DownloadDate.ToString("yyyy-MM-dd HH:mm");
            return true;
        }

        // Check playlist tracks
        foreach (var playlist in playlistsRecords)
        {
            var track = playlist.PlaylistTracks.FirstOrDefault(t =>
                t.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase));
            if (track != null)
            {
                downloadDate = track.DownloadDate != DateTime.MinValue
                    ? track.DownloadDate.ToString("yyyy-MM-dd HH:mm")
                    : playlist.DownloadDate.ToString("yyyy-MM-dd HH:mm");
                return true;
            }
        }

        return false;
    }
}
