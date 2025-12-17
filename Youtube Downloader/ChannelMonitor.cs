using System.Text.Json;
using System.Text.Json.Serialization;

namespace Youtube_Downloader;

/// <summary>
/// Represents a monitored YouTube channel
/// </summary>
public class MonitoredChannel
{
    public string ChannelId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string ChannelUrl { get; set; } = "";
    public string BannerPath { get; set; } = "";  // Local path to downloaded banner
    public DateTime DateAdded { get; set; } = DateTime.Now;
    public DateTime LastChecked { get; set; } = DateTime.MinValue;
    public DateTime MonitorFromDate { get; set; } = DateTime.MinValue;  // Only show videos after this date
    public DateTime BannerLastUpdated { get; set; } = DateTime.MinValue;  // When banner was last downloaded
    public string BannerETag { get; set; } = "";  // ETag for banner caching (future use)
    public List<ChannelVideo> Videos { get; set; } = new();
}

/// <summary>
/// Represents a video from a monitored channel
/// </summary>
public class ChannelVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public DateTime UploadDate { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsNew { get; set; } = true;  // True until user marks as played or ignored
    public VideoStatus Status { get; set; } = VideoStatus.New;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoStatus
{
    New,        // Just discovered, not yet acted upon
    Watched,    // User clicked to play/view in browser
    Downloaded, // Already downloaded
    Snoozed,    // User acknowledged but didn't watch - no alert but visible
    Ignored     // User chose to ignore - always hidden unless Show All Videos
}

/// <summary>
/// Manages storage and retrieval of monitored channels
/// </summary>
public class ChannelMonitorStorage
{
    private const string DataFileName = "channel_monitor.json";
    private readonly string dataPath;
    private List<MonitoredChannel> channels = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public IReadOnlyList<MonitoredChannel> Channels => channels.AsReadOnly();

    /// <summary>
    /// Returns a snapshot (copy) of the channels list for safe iteration
    /// when the collection might be modified during enumeration.
    /// </summary>
    public List<MonitoredChannel> GetChannelsSnapshot() => channels.ToList();

    public ChannelMonitorStorage()
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeDownloader");

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        dataPath = Path.Combine(appDataDir, DataFileName);
        Load();
    }

    public void Load()
    {
        channels.Clear();

        if (!File.Exists(dataPath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(dataPath);
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
            {
                return;
            }

            channels = JsonSerializer.Deserialize<List<MonitoredChannel>>(json, JsonOptions)
                       ?? new List<MonitoredChannel>();
        }
        catch
        {
            // If file is corrupted, start fresh
            channels.Clear();
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(channels, JsonOptions);
            File.WriteAllText(dataPath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    public void AddChannel(MonitoredChannel channel)
    {
        // Check if channel already exists
        var existing = channels.FirstOrDefault(c =>
            c.ChannelId.Equals(channel.ChannelId, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            channels.Add(channel);
            Save();
        }
    }

    public void RemoveChannel(string channelId)
    {
        var channel = channels.FirstOrDefault(c =>
            c.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));

        if (channel != null)
        {
            // Delete banner file if exists
            if (!string.IsNullOrEmpty(channel.BannerPath) && File.Exists(channel.BannerPath))
            {
                try { File.Delete(channel.BannerPath); } catch { }
            }

            channels.Remove(channel);
            Save();
        }
    }

    public MonitoredChannel? GetChannel(string channelId)
    {
        return channels.FirstOrDefault(c =>
            c.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasChannel(string channelId)
    {
        return channels.Any(c =>
            c.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateChannel(MonitoredChannel channel)
    {
        var index = channels.FindIndex(c =>
            c.ChannelId.Equals(channel.ChannelId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            channels[index] = channel;
            Save();
        }
    }

    public int GetNewVideoCount()
    {
        return channels.Sum(c => c.Videos.Count(v => v.Status == VideoStatus.New));
    }

    public List<(MonitoredChannel Channel, ChannelVideo Video)> GetAllNewVideos()
    {
        var newVideos = new List<(MonitoredChannel, ChannelVideo)>();

        foreach (var channel in channels)
        {
            foreach (var video in channel.Videos.Where(v => v.Status == VideoStatus.New))
            {
                newVideos.Add((channel, video));
            }
        }

        return newVideos.OrderByDescending(x => x.Item2.UploadDate).ToList();
    }

    /// <summary>
    /// Gets the path to store channel banners
    /// </summary>
    public string GetBannerStoragePath()
    {
        string bannerDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeDownloader", "ChannelBanners");

        if (!Directory.Exists(bannerDir))
        {
            Directory.CreateDirectory(bannerDir);
        }

        return bannerDir;
    }

    /// <summary>
    /// Check if a video ID exists in the download history
    /// </summary>
    public bool IsVideoInDownloadHistory(string videoId, DownloadHistory? history)
    {
        if (history == null || string.IsNullOrEmpty(videoId))
            return false;

        // Check singles history
        if (history.SingleRecords.Any(r => r.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check playlist tracks
        foreach (var playlist in history.PlaylistRecords)
        {
            if (playlist.PlaylistTracks.Any(t => t.VideoId.Equals(videoId, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Reset a channel - keep Ignored videos, clear all others, reset LastChecked
    /// </summary>
    public void ResetChannel(string channelId, bool includeIgnored = false)
    {
        var channel = GetChannel(channelId);
        if (channel != null)
        {
            if (includeIgnored)
            {
                // Clear ALL videos including ignored
                channel.Videos.Clear();
            }
            else
            {
                // Keep only Ignored videos
                channel.Videos = channel.Videos.Where(v => v.Status == VideoStatus.Ignored).ToList();
            }
            channel.LastChecked = DateTime.MinValue;
            Save();
        }
    }
}
