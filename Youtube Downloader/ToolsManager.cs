using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;

namespace Youtube_Downloader;

public class ToolsManager
{
    private readonly string sourcePath;
    private readonly Config config;

    // Get URLs from config (which has defaults if not set)
    private string YtDlpUrl => config.YtDlpDownloadUrl;
    private string FfmpegUrl => config.FfmpegDownloadUrl;
    private string DenoUrl => config.DenoDownloadUrl;
    private string WebView2Url => config.WebView2DownloadUrl;

    public string YtDlpPath { get; private set; } = "";
    public string FfmpegPath { get; private set; } = "";
    public string DenoPath { get; private set; } = "";

    public ToolsManager(Config config)
    {
        this.config = config;

        // Source folder is relative to application directory
        string appDir = AppPaths.AppDirectory;

        // For deployed/published app, Source will be beside the exe
        string deploySourcePath = Path.Combine(appDir, "Source");

        // For development, check if we're in a bin folder and go up to project root
        string devSourcePath = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "Source"));

        // Use dev path if it exists, otherwise use deploy path
        if (Directory.Exists(devSourcePath))
        {
            sourcePath = devSourcePath;
        }
        else
        {
            sourcePath = deploySourcePath;
            Directory.CreateDirectory(sourcePath);
        }

        // Load paths from config first
        if (!string.IsNullOrEmpty(config.YtDlpPath) && File.Exists(config.YtDlpPath))
        {
            YtDlpPath = config.YtDlpPath;
        }
        else
        {
            YtDlpPath = Path.Combine(sourcePath, "yt-dlp.exe");
        }

        if (!string.IsNullOrEmpty(config.FfmpegPath) && File.Exists(Path.Combine(config.FfmpegPath, "ffmpeg.exe")))
        {
            FfmpegPath = config.FfmpegPath;
        }
        else
        {
            // Try to find existing ffmpeg
            string? existingFfmpeg = FindFfmpegPath();
            if (existingFfmpeg != null)
            {
                FfmpegPath = existingFfmpeg;
            }
        }

        // Load Deno path from config or default location
        if (!string.IsNullOrEmpty(config.DenoPath) && File.Exists(config.DenoPath))
        {
            DenoPath = config.DenoPath;
        }
        else
        {
            DenoPath = Path.Combine(sourcePath, "deno.exe");
        }
    }

    public bool AreToolsAvailable()
    {
        return File.Exists(YtDlpPath) &&
               !string.IsNullOrEmpty(FfmpegPath) &&
               File.Exists(Path.Combine(FfmpegPath, "ffmpeg.exe"));
    }

    public bool IsDenoAvailable()
    {
        return File.Exists(DenoPath);
    }

    /// <summary>
    /// Checks if WebView2 Runtime is installed on the system.
    /// </summary>
    public static bool IsWebView2Available()
    {
        try
        {
            // Check registry for WebView2 installation
            // WebView2 can be installed per-user or per-machine
            string[] registryPaths = new[]
            {
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
            };

            foreach (var path in registryPaths)
            {
                // Check HKEY_LOCAL_MACHINE
                using var lmKey = Registry.LocalMachine.OpenSubKey(path);
                if (lmKey != null)
                {
                    var version = lmKey.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version) && version != "0.0.0.0")
                        return true;
                }

                // Check HKEY_CURRENT_USER
                using var cuKey = Registry.CurrentUser.OpenSubKey(path);
                if (cuKey != null)
                {
                    var version = cuKey.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version) && version != "0.0.0.0")
                        return true;
                }
            }

            return false;
        }
        catch
        {
            // If we can't check registry, assume not installed
            return false;
        }
    }

    /// <summary>
    /// Gets the installed WebView2 version, or null if not installed.
    /// </summary>
    public static string? GetWebView2Version()
    {
        try
        {
            string[] registryPaths = new[]
            {
                @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
            };

            foreach (var path in registryPaths)
            {
                using var lmKey = Registry.LocalMachine.OpenSubKey(path);
                if (lmKey != null)
                {
                    var version = lmKey.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version) && version != "0.0.0.0")
                        return version;
                }

                using var cuKey = Registry.CurrentUser.OpenSubKey(path);
                if (cuKey != null)
                {
                    var version = cuKey.GetValue("pv") as string;
                    if (!string.IsNullOrEmpty(version) && version != "0.0.0.0")
                        return version;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads and installs the WebView2 Runtime using the Evergreen Bootstrapper.
    /// </summary>
    public async Task<bool> InstallWebView2Async(IProgress<string>? progress = null)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            progress?.Report("Downloading WebView2 installer...");

            using var response = await client.GetAsync(WebView2Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    int percent = (int)(totalRead * 100 / totalBytes);
                    progress?.Report($"Downloading WebView2 installer... {percent}%");
                }
            }

            fileStream.Close();

            progress?.Report("Installing WebView2 Runtime...");

            // Run the installer silently
            var startInfo = new ProcessStartInfo
            {
                FileName = tempFile,
                Arguments = "/silent /install",
                UseShellExecute = true,
                Verb = "runas" // Request elevation
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    progress?.Report("WebView2 Runtime installed successfully");
                    return true;
                }
                else
                {
                    progress?.Report($"WebView2 installation failed (exit code: {process.ExitCode})");
                    return false;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error installing WebView2: {ex.Message}");
            return false;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }

    public void SetYtDlpPath(string path)
    {
        if (File.Exists(path))
        {
            YtDlpPath = path;
            config.YtDlpPath = path;
            config.Save();
        }
    }

    public void SetFfmpegPath(string path)
    {
        if (Directory.Exists(path) && File.Exists(Path.Combine(path, "ffmpeg.exe")))
        {
            FfmpegPath = path;
            config.FfmpegPath = path;
            config.Save();
        }
    }

    public void SetDenoPath(string path)
    {
        if (File.Exists(path))
        {
            DenoPath = path;
            config.DenoPath = path;
            config.Save();
        }
    }

    public async Task<bool> EnsureToolsAvailableAsync(IProgress<string>? progress = null)
    {
        try
        {
            // Check and download yt-dlp
            if (!File.Exists(YtDlpPath))
            {
                YtDlpPath = Path.Combine(sourcePath, "yt-dlp.exe");
                progress?.Report("Downloading yt-dlp.exe...");
                await DownloadYtDlpAsync(progress);
            }

            if (!File.Exists(YtDlpPath))
            {
                return false;
            }

            // Save yt-dlp path to config
            config.YtDlpPath = YtDlpPath;
            config.Save();

            // Check and download ffmpeg
            if (string.IsNullOrEmpty(FfmpegPath) || !File.Exists(Path.Combine(FfmpegPath, "ffmpeg.exe")))
            {
                string? ffmpegBinPath = FindFfmpegPath();
                if (ffmpegBinPath == null)
                {
                    progress?.Report("Downloading ffmpeg...");
                    await DownloadFfmpegAsync(progress);
                    ffmpegBinPath = FindFfmpegPath();
                }

                if (ffmpegBinPath == null)
                {
                    return false;
                }

                FfmpegPath = ffmpegBinPath;
            }

            // Save ffmpeg path to config
            config.FfmpegPath = FfmpegPath;
            config.Save();

            // Check and download Deno (optional but recommended for YouTube)
            if (!File.Exists(DenoPath))
            {
                DenoPath = Path.Combine(sourcePath, "deno.exe");
                progress?.Report("Downloading Deno runtime...");
                await DownloadDenoAsync(progress);
            }

            if (File.Exists(DenoPath))
            {
                config.DenoPath = DenoPath;
                config.Save();
            }

            return true;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RedownloadYtDlpAsync(IProgress<string>? progress = null)
    {
        try
        {
            // Delete existing yt-dlp if it exists
            if (File.Exists(YtDlpPath))
            {
                progress?.Report("Removing existing yt-dlp...");
                File.Delete(YtDlpPath);
            }

            YtDlpPath = Path.Combine(sourcePath, "yt-dlp.exe");
            await DownloadYtDlpAsync(progress);

            if (File.Exists(YtDlpPath))
            {
                config.YtDlpPath = YtDlpPath;
                config.Save();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RedownloadFfmpegAsync(IProgress<string>? progress = null)
    {
        try
        {
            // Delete existing ffmpeg folder if it exists
            if (!string.IsNullOrEmpty(FfmpegPath))
            {
                string ffmpegFolder = Path.GetDirectoryName(FfmpegPath) ?? "";
                if (Directory.Exists(ffmpegFolder) && ffmpegFolder.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report("Removing existing ffmpeg...");
                    try
                    {
                        Directory.Delete(ffmpegFolder, true);
                    }
                    catch
                    {
                        // Ignore deletion errors, will download anyway
                    }
                }
            }

            // Also try to delete based on config folder name
            if (!string.IsNullOrEmpty(config.FfmpegFolderName))
            {
                string oldFolder = Path.Combine(sourcePath, config.FfmpegFolderName);
                if (Directory.Exists(oldFolder))
                {
                    try
                    {
                        Directory.Delete(oldFolder, true);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }

            FfmpegPath = "";
            config.FfmpegPath = "";
            config.FfmpegFolderName = "";
            config.Save();

            await DownloadFfmpegAsync(progress);

            string? ffmpegBinPath = FindFfmpegPath();
            if (ffmpegBinPath != null)
            {
                FfmpegPath = ffmpegBinPath;
                config.FfmpegPath = FfmpegPath;
                config.Save();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RedownloadDenoAsync(IProgress<string>? progress = null)
    {
        try
        {
            // Delete existing deno if it exists
            if (File.Exists(DenoPath))
            {
                progress?.Report("Removing existing Deno...");
                File.Delete(DenoPath);
            }

            DenoPath = Path.Combine(sourcePath, "deno.exe");
            await DownloadDenoAsync(progress);

            if (File.Exists(DenoPath))
            {
                config.DenoPath = DenoPath;
                config.Save();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            progress?.Report($"Error: {ex.Message}");
            return false;
        }
    }

    private string? FindFfmpegPath()
    {
        // First check if we have a configured folder name
        if (!string.IsNullOrEmpty(config.FfmpegFolderName))
        {
            string configuredPath = Path.Combine(sourcePath, config.FfmpegFolderName, "bin");
            if (Directory.Exists(configuredPath) && File.Exists(Path.Combine(configuredPath, "ffmpeg.exe")))
            {
                return configuredPath;
            }
        }

        // Search for any ffmpeg folder in Source
        if (Directory.Exists(sourcePath))
        {
            var ffmpegDirs = Directory.GetDirectories(sourcePath, "ffmpeg*");
            foreach (var dir in ffmpegDirs)
            {
                string binPath = Path.Combine(dir, "bin");
                if (Directory.Exists(binPath) && File.Exists(Path.Combine(binPath, "ffmpeg.exe")))
                {
                    // Update config with the found folder name
                    config.FfmpegFolderName = Path.GetFileName(dir);
                    config.Save();
                    return binPath;
                }
            }
        }

        return null;
    }

    private async Task DownloadYtDlpAsync(IProgress<string>? progress)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10);

        progress?.Report("Downloading yt-dlp.exe...");

        using var response = await client.GetAsync(YtDlpUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(YtDlpPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                int percent = (int)(totalRead * 100 / totalBytes);
                progress?.Report($"Downloading yt-dlp.exe... {percent}%");
            }
        }

        progress?.Report("yt-dlp.exe downloaded successfully");
    }

    private async Task DownloadFfmpegAsync(IProgress<string>? progress)
    {
        string tempFile = Path.Combine(sourcePath, "ffmpeg-temp.7z");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(30);

            progress?.Report("Downloading ffmpeg...");

            using var response = await client.GetAsync(FfmpegUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    int percent = (int)(totalRead * 100 / totalBytes);
                    progress?.Report($"Downloading ffmpeg... {percent}%");
                }
            }

            fileStream.Close();

            progress?.Report("Extracting ffmpeg...");

            string? extractedFolderName = null;

            // Extract the 7z file - use explicit using block to ensure disposal
            using (var archive = ArchiveFactory.Open(tempFile))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        // Get the root folder name from the first entry
                        if (extractedFolderName == null)
                        {
                            var parts = entry.Key?.Split('/');
                            if (parts != null && parts.Length > 0)
                            {
                                extractedFolderName = parts[0];
                            }
                        }

                        entry.WriteToDirectory(sourcePath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            } // Archive disposed here

            // Update config with the extracted folder name
            if (!string.IsNullOrEmpty(extractedFolderName))
            {
                config.FfmpegFolderName = extractedFolderName;
                config.Save();
            }

            progress?.Report("ffmpeg extracted successfully");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }

            // Force garbage collection to release memory from extraction
            // Compact the Large Object Heap to reclaim memory from large buffers
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        }
    }

    private async Task DownloadDenoAsync(IProgress<string>? progress)
    {
        string tempFile = Path.Combine(sourcePath, "deno-temp.zip");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            progress?.Report("Downloading Deno runtime...");

            using var response = await client.GetAsync(DenoUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    int percent = (int)(totalRead * 100 / totalBytes);
                    progress?.Report($"Downloading Deno runtime... {percent}%");
                }
            }

            fileStream.Close();

            progress?.Report("Extracting Deno...");

            // Extract only deno.exe from the zip
            using (var archive = ArchiveFactory.Open(tempFile))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory && entry.Key != null &&
                        entry.Key.Equals("deno.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.WriteToDirectory(sourcePath, new ExtractionOptions
                        {
                            ExtractFullPath = false,
                            Overwrite = true
                        });
                        break;
                    }
                }
            }

            progress?.Report("Deno runtime downloaded successfully");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
