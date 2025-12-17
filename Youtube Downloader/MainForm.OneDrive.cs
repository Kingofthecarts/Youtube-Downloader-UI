using System.Diagnostics;

namespace Youtube_Downloader;

/// <summary>
/// Partial class containing OneDrive monitoring methods for MainForm.
/// </summary>
public partial class MainForm
{
    private void StartOneDriveMonitoring()
    {
        if (oneDriveTimer != null) return;

        oneDriveTimer = new System.Windows.Forms.Timer();
        oneDriveTimer.Interval = 5 * 60 * 1000; // 5 minutes
        oneDriveTimer.Tick += (s, e) => CheckOneDriveStatus(showSuccessMessage: false);
        oneDriveTimer.Start();
        logger.Log("OneDrive monitoring started (5 minute interval)");
    }

    private void StopOneDriveMonitoring()
    {
        if (oneDriveTimer != null)
        {
            oneDriveTimer.Stop();
            oneDriveTimer.Dispose();
            oneDriveTimer = null;
            logger.Log("OneDrive monitoring stopped");
        }
    }

    private void CheckOneDriveStatus(bool showSuccessMessage = false)
    {
        var oneDriveStatus = GetOneDriveStatus();

        if (!oneDriveStatus.IsRunning)
        {
            logger.Log("OneDrive is not running");
            var result = MessageBox.Show(
                "OneDrive is not running!\n\n" +
                "Your files may not be syncing to the cloud.\n\n" +
                "Would you like to start OneDrive now?",
                "OneDrive Not Running",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                StartOneDrive();
            }
        }
        else if (!oneDriveStatus.IsSyncing)
        {
            logger.Log($"OneDrive is running but not syncing (Status: {oneDriveStatus.Status})");
            MessageBox.Show(
                $"OneDrive is running but may not be syncing properly.\n\n" +
                $"Status: {oneDriveStatus.Status}\n\n" +
                "Please check OneDrive to ensure your files are being backed up.",
                "OneDrive Sync Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else if (showSuccessMessage)
        {
            MessageBox.Show(
                "OneDrive is running and syncing normally.",
                "OneDrive Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void StartOneDrive()
    {
        try
        {
            // Try common OneDrive paths
            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive", "OneDrive.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive", "OneDrive.exe")
            };

            string? oneDrivePath = possiblePaths.FirstOrDefault(File.Exists);

            if (oneDrivePath != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = oneDrivePath,
                    UseShellExecute = true
                });
                logger.Log($"Started OneDrive from: {oneDrivePath}");
            }
            else
            {
                MessageBox.Show(
                    "Could not find OneDrive installation.\n\n" +
                    "Please start OneDrive manually from the Start menu.",
                    "OneDrive Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to start OneDrive: {ex.Message}");
            MessageBox.Show(
                $"Failed to start OneDrive: {ex.Message}\n\n" +
                "Please start OneDrive manually.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private (bool IsRunning, bool IsSyncing, string Status) GetOneDriveStatus()
    {
        try
        {
            // Check if OneDrive process is running
            var oneDriveProcesses = Process.GetProcessesByName("OneDrive");
            bool isRunning = oneDriveProcesses.Length > 0;

            if (!isRunning)
            {
                return (false, false, "Not Running");
            }

            // OneDrive is running - assume it's syncing if the process is active
            // More detailed status would require COM interop or registry checks
            // For now, if OneDrive.exe is running, we consider it as potentially syncing
            return (true, true, "Running");
        }
        catch (Exception ex)
        {
            logger.Log($"Error checking OneDrive status: {ex.Message}");
            return (false, false, $"Error: {ex.Message}");
        }
    }
}
