namespace Youtube_Downloader;

/// <summary>
/// Singleton that holds the shared MediaPlayerManager instance.
/// This allows music to continue playing when SongBrowser is closed,
/// and enables mirroring between MainForm and SongBrowserForm player controls.
/// </summary>
public static class SharedMediaPlayer
{
    private static MediaPlayerManager? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the shared MediaPlayerManager instance, creating it if needed.
    /// </summary>
    public static MediaPlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new MediaPlayerManager();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Event raised when a track starts playing (includes file path and title).
    /// </summary>
    public static event EventHandler<TrackStartedEventArgs>? TrackStarted;

    /// <summary>
    /// Event raised when playback is stopped (manually via Stop()).
    /// </summary>
    public static event EventHandler? Stopped;

    /// <summary>
    /// Plays a track and notifies all listeners.
    /// </summary>
    public static void Play(string filePath, string? title = null)
    {
        Instance.Play(filePath, title);
        TrackStarted?.Invoke(null, new TrackStartedEventArgs(filePath, title ?? System.IO.Path.GetFileNameWithoutExtension(filePath)));
    }

    /// <summary>
    /// Stops playback and notifies all listeners.
    /// </summary>
    public static void Stop()
    {
        Instance.Stop();
        Stopped?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Gets whether music is currently playing.
    /// </summary>
    public static bool IsPlaying => Instance.IsPlaying;

    /// <summary>
    /// Gets whether a track is loaded.
    /// </summary>
    public static bool HasTrack => Instance.HasTrack;
}

/// <summary>
/// Event args for when a track starts playing.
/// </summary>
public class TrackStartedEventArgs : EventArgs
{
    public string FilePath { get; }
    public string Title { get; }

    public TrackStartedEventArgs(string filePath, string title)
    {
        FilePath = filePath;
        Title = title;
    }
}
