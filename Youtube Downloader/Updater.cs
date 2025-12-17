using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Youtube_Downloader;

/// <summary>
/// Handles application auto-update from a GitHub release URL.
///
/// CODE SIGNING NOTES:
/// To sign the executable for distribution, you need a code signing certificate:
///
/// Option 1: Certificate from a Certificate Authority (CA)
/// - Purchase a code signing certificate from a trusted CA such as:
///   * DigiCert (digicert.com)
///   * Sectigo (sectigo.com)
///   * GlobalSign (globalsign.com)
/// - This provides the highest trust level and Windows SmartScreen compatibility.
///
/// Option 2: Self-signed certificate (for internal/personal use)
/// - Create a self-signed certificate using PowerShell:
///   New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Your Name" -CertStoreLocation Cert:\CurrentUser\My
/// - Export to PFX file with private key
/// - Users will need to trust your certificate manually
///
/// To sign the executable:
/// signtool sign /f certificate.pfx /p password /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 "Youtube Downloader.exe"
///
/// The /tr and /td parameters add a timestamp so the signature remains valid after the certificate expires.
/// </summary>
public static class AppUpdater
{
    private const string UpdateFolderName = "YTDownloaderUpdate";
    private const long MaxFileSizeBytes = 200 * 1024 * 1024; // 200 MB max

    /// <summary>
    /// Gets the path to the update folder in the user's temp directory.
    /// </summary>
    public static string UpdateFolder => Path.Combine(Path.GetTempPath(), UpdateFolderName);

    /// <summary>
    /// Gets the current application version from the assembly.
    /// </summary>
    public static Version CurrentVersion
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(1, 0, 0);
        }
    }

    /// <summary>
    /// Gets the current version as a display string (e.g., "1.0.0").
    /// </summary>
    public static string CurrentVersionString
    {
        get
        {
            var v = CurrentVersion;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    /// <summary>
    /// Result of checking for updates.
    /// </summary>
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string? LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Checks GitHub releases for a newer version.
    /// </summary>
    /// <param name="repoUrl">GitHub repository URL (e.g., https://github.com/user/repo)</param>
    /// <returns>Update check result with version info and download URL if available.</returns>
    public static async Task<UpdateCheckResult> CheckForUpdateAsync(string repoUrl)
    {
        var result = new UpdateCheckResult();

        try
        {
            // Parse the repo URL to extract owner/repo
            // Expected format: https://github.com/owner/repo or https://github.com/owner/repo/releases
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                result.ErrorMessage = "GitHub repository URL is not configured.";
                return result;
            }

            var uri = new Uri(repoUrl);
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length < 2)
            {
                result.ErrorMessage = "Invalid GitHub repository URL format.";
                return result;
            }

            string owner = pathParts[0];
            string repo = pathParts[1];

            // Use GitHub API to get latest release
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "YouTube-Downloader-App");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var response = await httpClient.GetAsync(apiUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                result.ErrorMessage = "No releases found for this repository.";
                return result;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Get tag name (version)
            string tagName = root.GetProperty("tag_name").GetString() ?? "";
            // Remove 'v' prefix if present
            string versionString = tagName.TrimStart('v', 'V');
            result.LatestVersion = versionString;

            // Get release notes
            if (root.TryGetProperty("body", out var bodyElement))
            {
                result.ReleaseNotes = bodyElement.GetString();
            }

            // Find the exe asset
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? assetName = asset.GetProperty("name").GetString();
                    if (assetName != null && assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        result.DownloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(result.DownloadUrl))
            {
                result.ErrorMessage = "No executable found in the latest release.";
                return result;
            }

            // Compare versions
            if (Version.TryParse(versionString, out var latestVersion))
            {
                result.UpdateAvailable = latestVersion > CurrentVersion;
            }
            else
            {
                // If we can't parse, assume update available if versions differ
                result.UpdateAvailable = versionString != CurrentVersionString;
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Network error: {ex.Message}";
            return result;
        }
        catch (JsonException ex)
        {
            result.ErrorMessage = $"Failed to parse GitHub response: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error checking for updates: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Downloads the new executable from the specified URL to a temp location.
    /// </summary>
    /// <param name="downloadUrl">The HTTPS URL to download the new exe from.</param>
    /// <param name="progress">Optional progress reporter (0-100 percent).</param>
    /// <returns>True if the download was successful, false otherwise.</returns>
    public static async Task<bool> CheckAndDownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
    {
        // Validate URL
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new ArgumentException("Download URL is required.", nameof(downloadUrl));
        }

        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format.", nameof(downloadUrl));
        }

        // Security: Require HTTPS
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("URL must use HTTPS for security.", nameof(downloadUrl));
        }

        // Create update folder
        if (Directory.Exists(UpdateFolder))
        {
            // Clean up any previous update files
            try
            {
                Directory.Delete(UpdateFolder, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        Directory.CreateDirectory(UpdateFolder);

        string downloadedExePath = Path.Combine(UpdateFolder, "Youtube Downloader.exe");

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            // First, check the file size with a HEAD request
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await httpClient.SendAsync(headRequest);
            headResponse.EnsureSuccessStatusCode();

            if (headResponse.Content.Headers.ContentLength.HasValue)
            {
                long contentLength = headResponse.Content.Headers.ContentLength.Value;
                if (contentLength > MaxFileSizeBytes)
                {
                    throw new InvalidOperationException($"File size ({contentLength / (1024 * 1024)} MB) exceeds maximum allowed size ({MaxFileSizeBytes / (1024 * 1024)} MB).");
                }
            }

            // Download the file with progress reporting
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            // Validate content length again from the actual download response
            if (totalBytes.HasValue && totalBytes.Value > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File size ({totalBytes.Value / (1024 * 1024)} MB) exceeds maximum allowed size ({MaxFileSizeBytes / (1024 * 1024)} MB).");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(downloadedExePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                // Verify we haven't exceeded max size during download
                if (totalRead > MaxFileSizeBytes)
                {
                    fileStream.Close();
                    File.Delete(downloadedExePath);
                    throw new InvalidOperationException("Downloaded file exceeded maximum allowed size.");
                }

                if (totalBytes.HasValue && progress != null)
                {
                    int percentComplete = (int)((totalRead * 100) / totalBytes.Value);
                    progress.Report(percentComplete);
                }
            }

            progress?.Report(100);

            // Verify the file was downloaded
            if (!File.Exists(downloadedExePath))
            {
                return false;
            }

            var fileInfo = new FileInfo(downloadedExePath);
            if (fileInfo.Length == 0)
            {
                File.Delete(downloadedExePath);
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            // Clean up on failure
            try
            {
                if (File.Exists(downloadedExePath))
                {
                    File.Delete(downloadedExePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            throw;
        }
    }

    /// <summary>
    /// Creates and launches a batch script that will:
    /// 1. Wait for the current application to close
    /// 2. Delete the old executable
    /// 3. Move the new executable to the application location
    /// 4. Start the new executable
    /// 5. Delete itself
    /// </summary>
    public static void LaunchUpdateScript()
    {
        string? currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
        {
            throw new InvalidOperationException("Unable to determine current executable path.");
        }

        string newExePath = Path.Combine(UpdateFolder, "Youtube Downloader.exe");
        if (!File.Exists(newExePath))
        {
            throw new FileNotFoundException("Downloaded update file not found.", newExePath);
        }

        string scriptPath = Path.Combine(UpdateFolder, "update.bat");

        // Create the batch script
        // Note: Using delayed expansion and careful quoting to handle paths with spaces
        string scriptContent = $@"@echo off
setlocal enabledelayedexpansion

:: Wait for the application to close (2 seconds)
timeout /t 2 /nobreak > nul

:: Define paths
set ""OLD_EXE={currentExePath}""
set ""NEW_EXE={newExePath}""

:: Attempt to delete the old exe (retry up to 5 times)
set retries=0
:retry_delete
if !retries! geq 5 (
    echo Failed to delete old executable after 5 attempts.
    pause
    goto :cleanup
)
del ""%OLD_EXE%"" 2>nul
if exist ""%OLD_EXE%"" (
    set /a retries+=1
    timeout /t 1 /nobreak > nul
    goto :retry_delete
)

:: Move the new exe to the old location
move /y ""%NEW_EXE%"" ""%OLD_EXE%"" > nul
if errorlevel 1 (
    echo Failed to move new executable.
    pause
    goto :cleanup
)

:: Start the new application
start """" ""%OLD_EXE%""

:cleanup
:: Delete the update folder contents and this script
:: Using a slight delay to ensure files are released
timeout /t 1 /nobreak > nul
rd /s /q ""{UpdateFolder}"" 2>nul

:: Delete this script (this line won't complete but the script will be deleted)
del ""%~f0"" 2>nul
";

        File.WriteAllText(scriptPath, scriptContent);

        // Launch the script hidden
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        Process.Start(startInfo);
    }

    /// <summary>
    /// Gets the path to the downloaded update file, if it exists.
    /// </summary>
    public static string? GetDownloadedUpdatePath()
    {
        string path = Path.Combine(UpdateFolder, "Youtube Downloader.exe");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Cleans up the update folder.
    /// </summary>
    public static void CleanupUpdateFolder()
    {
        try
        {
            if (Directory.Exists(UpdateFolder))
            {
                Directory.Delete(UpdateFolder, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
