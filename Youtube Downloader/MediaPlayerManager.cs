using NAudio.Wave;

namespace Youtube_Downloader;

public class MediaPlayerManager : IDisposable
{
    private WaveOutEvent? outputDevice;
    private AudioFileReader? audioFile;
    private System.Windows.Forms.Timer? positionTimer;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler? PlaybackStopped;
    public event EventHandler? TrackEnded;

    public string? CurrentFile { get; private set; }
    public string? CurrentTitle { get; private set; }
    public TimeSpan Duration => audioFile?.TotalTime ?? TimeSpan.Zero;
    public TimeSpan Position => audioFile?.CurrentTime ?? TimeSpan.Zero;
    public bool IsPlaying => outputDevice?.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => outputDevice?.PlaybackState == PlaybackState.Paused;
    public bool HasTrack => audioFile != null;

    public float Volume
    {
        get => outputDevice?.Volume ?? 1.0f;
        set
        {
            if (outputDevice != null)
                outputDevice.Volume = Math.Clamp(value, 0f, 1f);
        }
    }

    public MediaPlayerManager()
    {
        positionTimer = new System.Windows.Forms.Timer();
        positionTimer.Interval = 250;
        positionTimer.Tick += PositionTimer_Tick;
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (audioFile != null)
        {
            PositionChanged?.Invoke(this, audioFile.CurrentTime);
        }
    }

    public void Play(string filePath, string? title = null)
    {
        Stop();

        try
        {
            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
            outputDevice.Init(audioFile);
            outputDevice.Play();

            CurrentFile = filePath;
            CurrentTitle = title ?? Path.GetFileNameWithoutExtension(filePath);
            positionTimer?.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to play file: {ex.Message}", "Playback Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Stop();
        }
    }

    /// <summary>
    /// Load a file and seek to position without playing yet (starts paused).
    /// Call Resume() to start playback.
    /// </summary>
    public void LoadPaused(string filePath, TimeSpan position, string? title = null)
    {
        Stop();

        try
        {
            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
            outputDevice.Init(audioFile);

            // Seek to position before any playback
            if (position > TimeSpan.Zero && position < audioFile.TotalTime)
            {
                audioFile.CurrentTime = position;
            }

            // Don't call Play() - leave it paused
            CurrentFile = filePath;
            CurrentTitle = title ?? Path.GetFileNameWithoutExtension(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load file: {ex.Message}", "Playback Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Stop();
        }
    }

    private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        positionTimer?.Stop();

        // Check if track ended naturally (position at or near end)
        if (audioFile != null && audioFile.CurrentTime >= audioFile.TotalTime - TimeSpan.FromMilliseconds(500))
        {
            TrackEnded?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Pause()
    {
        if (outputDevice?.PlaybackState == PlaybackState.Playing)
        {
            outputDevice.Pause();
            positionTimer?.Stop();
        }
    }

    public void Resume()
    {
        if (outputDevice?.PlaybackState == PlaybackState.Paused)
        {
            outputDevice.Play();
            positionTimer?.Start();
        }
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
            Pause();
        else if (IsPaused)
            Resume();
    }

    public void Stop()
    {
        positionTimer?.Stop();

        if (outputDevice != null)
        {
            outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;
            outputDevice.Stop();
            outputDevice.Dispose();
            outputDevice = null;
        }

        if (audioFile != null)
        {
            audioFile.Dispose();
            audioFile = null;
        }

        CurrentFile = null;
        CurrentTitle = null;
    }

    public void Seek(TimeSpan position)
    {
        if (audioFile != null)
        {
            audioFile.CurrentTime = position;
            PositionChanged?.Invoke(this, position);
        }
    }

    public void SeekPercent(double percent)
    {
        if (audioFile != null)
        {
            var newPosition = TimeSpan.FromSeconds(audioFile.TotalTime.TotalSeconds * percent);
            Seek(newPosition);
        }
    }

    public void Dispose()
    {
        Stop();
        positionTimer?.Dispose();
        positionTimer = null;
    }
}
