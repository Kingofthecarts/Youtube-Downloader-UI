namespace Youtube_Downloader;

public class ToolsDownloadForm : Form
{
    private Label titleLabel = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private Button cancelButton = null!;

    private readonly ToolsManager toolsManager;
    private CancellationTokenSource? cancellationTokenSource;
    private bool downloadComplete = false;
    private bool isCancelling = false;

    public ToolsDownloadForm(ToolsManager toolsManager)
    {
        this.toolsManager = toolsManager;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Text = "Downloading Required Tools";
        Size = new Size(450, 160);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;

        titleLabel = new Label
        {
            Text = "Downloading yt-dlp and ffmpeg",
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

        Load += ToolsDownloadForm_Load;
    }

    private void UpdateStatus(string msg)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateStatus(msg));
            return;
        }

        statusLabel.Text = msg;

        // Parse percentage if present
        if (msg.Contains('%'))
        {
            var match = System.Text.RegularExpressions.Regex.Match(msg, @"(\d+)%");
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

    private async void ToolsDownloadForm_Load(object? sender, EventArgs e)
    {
        cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<string>(UpdateStatus);

        try
        {
            bool success = await toolsManager.EnsureToolsAvailableAsync(progress);

            if (success)
            {
                downloadComplete = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (!isCancelling)
            {
                statusLabel.Text = "Download failed. Please try again.";
                cancelButton.Text = "Close";
                downloadComplete = true; // Allow closing
            }
        }
        catch (OperationCanceledException)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error: {ex.Message}";
            cancelButton.Text = "Close";
            downloadComplete = true; // Allow closing
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        if (!downloadComplete)
        {
            isCancelling = true;
            cancellationTokenSource?.Cancel();
            DialogResult = DialogResult.Cancel;
            Close();
        }
        else
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!downloadComplete && !isCancelling)
        {
            e.Cancel = true;
            return;
        }
        cancellationTokenSource?.Dispose();
        base.OnFormClosing(e);
    }
}
