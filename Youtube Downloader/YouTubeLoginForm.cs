using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Youtube_Downloader;

public partial class YouTubeLoginForm : Form
{
    private readonly Config config;
    private WebView2 webView;
    private Button doneButton;
    private Button cancelButton;
    private Label statusLabel;
    private Label instructionLabel;
    private bool isInitialized;

    public bool LoginSuccessful { get; private set; }

    public YouTubeLoginForm(Config config)
    {
        this.config = config;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Sign in to YouTube";
        this.Size = new Size(800, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.MinimumSize = new Size(600, 500);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;

        // Instruction label at top
        instructionLabel = new Label
        {
            Text = "Sign in to your Google account below, then click 'Done' when finished.",
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 9.5f),
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(10)
        };

        // WebView2 control
        webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        // Bottom panel for buttons
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            Padding = new Padding(10)
        };

        statusLabel = new Label
        {
            Text = "Loading...",
            AutoSize = true,
            Location = new Point(10, 15),
            ForeColor = Color.Gray
        };

        doneButton = new Button
        {
            Text = "Done",
            Width = 100,
            Height = 30,
            Enabled = false
        };
        doneButton.Click += DoneButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Width = 100,
            Height = 30
        };
        cancelButton.Click += CancelButton_Click;

        // Position buttons on right side
        buttonPanel.Resize += (s, e) =>
        {
            cancelButton.Location = new Point(buttonPanel.Width - cancelButton.Width - 10, 10);
            doneButton.Location = new Point(cancelButton.Left - doneButton.Width - 10, 10);
        };

        buttonPanel.Controls.Add(statusLabel);
        buttonPanel.Controls.Add(doneButton);
        buttonPanel.Controls.Add(cancelButton);

        this.Controls.Add(webView);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(instructionLabel);

        this.Load += YouTubeLoginForm_Load;
    }

    private async void YouTubeLoginForm_Load(object? sender, EventArgs e)
    {
        try
        {
            // Set up WebView2 user data folder in AppData
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YoutubeDownloader",
                "WebView2Data");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            isInitialized = true;
            statusLabel.Text = "Ready - Sign in to your Google account";
            doneButton.Enabled = true;

            // Navigate to YouTube login
            webView.CoreWebView2.Navigate("https://accounts.google.com/ServiceLogin?service=youtube&continue=https://www.youtube.com");

            // Track navigation to update status
            webView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                string url = webView.CoreWebView2.Source;
                if (url.Contains("youtube.com") && !url.Contains("accounts.google.com"))
                {
                    statusLabel.Text = "Signed in - Click 'Done' to save";
                    statusLabel.ForeColor = Color.Green;
                }
                else if (url.Contains("accounts.google.com"))
                {
                    statusLabel.Text = "Enter your Google credentials...";
                    statusLabel.ForeColor = Color.Gray;
                }
            };
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"Error: {ex.Message}";
            statusLabel.ForeColor = Color.Red;
            MessageBox.Show(
                $"Failed to initialize browser:\n\n{ex.Message}\n\nMake sure WebView2 Runtime is installed.",
                "Browser Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void DoneButton_Click(object? sender, EventArgs e)
    {
        if (!isInitialized || webView.CoreWebView2 == null)
        {
            MessageBox.Show("Browser not ready. Please wait.", "Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            statusLabel.Text = "Extracting cookies...";
            doneButton.Enabled = false;
            cancelButton.Enabled = false;

            // Get all cookies from WebView2
            var cookieManager = webView.CoreWebView2.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync("https://www.youtube.com");

            if (cookies.Count == 0)
            {
                MessageBox.Show(
                    "No cookies found. Please make sure you've signed in to YouTube.",
                    "No Cookies",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                doneButton.Enabled = true;
                cancelButton.Enabled = true;
                statusLabel.Text = "Sign in to YouTube first";
                return;
            }

            // Convert to our format
            var cookieList = new List<CookieData>();
            foreach (var cookie in cookies)
            {
                cookieList.Add(new CookieData
                {
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Name = cookie.Name,
                    Value = cookie.Value,
                    IsSecure = cookie.IsSecure,
                    Expires = new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds()
                });
            }

            // Also get Google cookies for authentication
            var googleCookies = await cookieManager.GetCookiesAsync("https://accounts.google.com");
            foreach (var cookie in googleCookies)
            {
                cookieList.Add(new CookieData
                {
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Name = cookie.Name,
                    Value = cookie.Value,
                    IsSecure = cookie.IsSecure,
                    Expires = new DateTimeOffset(cookie.Expires).ToUnixTimeSeconds()
                });
            }

            // Convert to Netscape format and save
            string cookieData = YouTubeCookieManager.ConvertToNetscapeFormat(cookieList);

            // Try to get the user's email from cookies or page
            string? email = null;
            // The SAPISID or LOGIN_INFO cookie might contain hints, but for simplicity
            // we'll leave email extraction for later or skip it

            YouTubeCookieManager.SaveCookies(config, cookieData, email);

            LoginSuccessful = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save cookies:\n\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            doneButton.Enabled = true;
            cancelButton.Enabled = true;
            statusLabel.Text = "Error - try again";
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // Clean up WebView2
        if (webView != null)
        {
            webView.Dispose();
        }
    }
}
