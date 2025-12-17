namespace Youtube_Downloader;

/// <summary>
/// Helper class to get the correct application directory for single-file deployments.
/// When published as a single file, the app extracts to a temp directory, so we need
/// to use the actual exe location instead of AppDomain.CurrentDomain.BaseDirectory.
/// </summary>
public static class AppPaths
{
    private static string? _appDirectory;

    /// <summary>
    /// Gets the directory where the application exe is located.
    /// Works correctly for both normal and single-file deployments.
    /// </summary>
    public static string AppDirectory
    {
        get
        {
            if (_appDirectory == null)
            {
                // For single-file apps, get the actual exe location
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    _appDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                }
            }
            return _appDirectory;
        }
    }
}
