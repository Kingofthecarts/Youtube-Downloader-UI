namespace Youtube_Downloader;

public class DownloadProgressForm : Form
{
    private Label titleLabel = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private Button cancelButton = null!;

    private CancellationTokenSource? cancellationTokenSource;
    private bool isComplete = false;
    private bool isUpdateMode = false;
    private Action? onUpgradeClicked;

    public bool WasCancelled { get; private set; } = false;
    public bool WasSuccessful { get; private set; } = false;

    public DownloadProgressForm(string title, bool updateMode = false)
    {
        isUpdateMode = updateMode;
        InitializeComponents(title);
    }

    private void InitializeComponents(string title)
    {
        Text = title;
        Size = new Size(450, 160);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;

        titleLabel = new Label
        {
            Text = title,
            Location = new Point(15, 15),
            Size = new Size(405, 20),
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
        };

        statusLabel = new Label
        {
            Text = "Initializing...",
            Location = new Point(15, 40),
            Size = new Size(405, 20),
            AutoEllipsis = true
        };

        progressBar = new ProgressBar
        {
            Location = new Point(15, 65),
            Size = new Size(405, 23),
            Style = ProgressBarStyle.Marquee
        };

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(345, 95),
            Size = new Size(75, 28)
        };
        cancelButton.Click += CancelButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(statusLabel);
        Controls.Add(progressBar);
        Controls.Add(cancelButton);
    }

    public void UpdateStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(status));
            return;
        }

        statusLabel.Text = status;

        // Parse percentage if present
        if (status.Contains('%'))
        {
            var match = System.Text.RegularExpressions.Regex.Match(status, @"(\d+)%");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int percent))
            {
                if (progressBar.Style != ProgressBarStyle.Continuous)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                }
                progressBar.Value = Math.Min(100, percent);
            }
        }
    }

    public void SetComplete(bool success)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetComplete(success));
            return;
        }

        isComplete = true;
        WasSuccessful = success;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = success ? 100 : 0;

        if (success && isUpdateMode)
        {
            // For updates, show "Install Update" button
            cancelButton.Text = "Install Update";
            cancelButton.BackColor = Color.FromArgb(0, 120, 212);
            cancelButton.ForeColor = Color.White;
            cancelButton.Font = new Font(cancelButton.Font, FontStyle.Bold);
            cancelButton.Size = new Size(100, 28);
            cancelButton.Location = new Point(320, 95);
        }
        else if (success)
        {
            // For regular downloads, auto-close after a brief delay
            Task.Delay(500).ContinueWith(_ =>
            {
                if (!IsDisposed)
                {
                    Invoke(() =>
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    });
                }
            });
        }
        else
        {
            cancelButton.Text = "Close";
        }
    }

    /// <summary>
    /// Sets the action to perform when the upgrade button is clicked.
    /// </summary>
    public void SetUpgradeAction(Action action)
    {
        onUpgradeClicked = action;
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (!isComplete)
        {
            WasCancelled = true;
            cancellationTokenSource?.Cancel();
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        if (WasSuccessful && isUpdateMode && onUpgradeClicked != null)
        {
            // Trigger the upgrade action
            DialogResult = DialogResult.OK;
            Close();
            onUpgradeClicked.Invoke();
            return;
        }

        DialogResult = WasSuccessful ? DialogResult.OK : DialogResult.Cancel;
        Close();
    }

    public async Task RunDownloadAsync(Func<IProgress<string>, CancellationToken, Task<bool>> downloadAction)
    {
        cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<string>(UpdateStatus);

        try
        {
            bool success = await downloadAction(progress, cancellationTokenSource.Token);
            SetComplete(success);

            if (success)
            {
                UpdateStatus("Download completed successfully");
            }
            else if (!WasCancelled)
            {
                UpdateStatus("Download failed");
            }
        }
        catch (OperationCanceledException)
        {
            WasCancelled = true;
            UpdateStatus("Cancelled");
            SetComplete(false);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            SetComplete(false);
        }
        finally
        {
            cancellationTokenSource?.Dispose();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!isComplete && !WasCancelled)
        {
            // Don't allow closing while download is in progress
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }
}
