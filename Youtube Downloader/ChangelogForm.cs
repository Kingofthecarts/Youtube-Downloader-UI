using System.Reflection;

namespace Youtube_Downloader;

public class ChangelogForm : Form
{
    public ChangelogForm()
    {
        Text = "Changelog";
        Size = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var richTextBox = new RichTextBox
        {
            Location = new Point(10, 10),
            Size = new Size(485, 390),
            ReadOnly = true,
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle
        };

        FormatChangelog(richTextBox);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(410, 410),
            Size = new Size(85, 28),
            DialogResult = DialogResult.OK
        };

        Controls.Add(richTextBox);
        Controls.Add(closeButton);

        AcceptButton = closeButton;
    }

    private static void FormatChangelog(RichTextBox rtb)
    {
        string markdown = LoadChangelog();
        var lines = markdown.Split('\n');

        foreach (var line in lines)
        {
            string trimmed = line.TrimEnd('\r');

            if (trimmed.StartsWith("# "))
            {
                // Main title - large bold
                AppendText(rtb, trimmed[2..] + "\n", new Font("Segoe UI", 16, FontStyle.Bold), Color.Black);
            }
            else if (trimmed.StartsWith("## "))
            {
                // Version header - medium bold blue
                if (rtb.TextLength > 0)
                    AppendText(rtb, "\n", rtb.Font, Color.Black);
                AppendText(rtb, trimmed[3..] + "\n", new Font("Segoe UI", 12, FontStyle.Bold), Color.DarkBlue);
            }
            else if (trimmed.StartsWith("- "))
            {
                // Bullet point
                AppendText(rtb, "  \u2022 ", new Font("Segoe UI", 10, FontStyle.Regular), Color.DarkGreen);
                AppendText(rtb, trimmed[2..] + "\n", new Font("Segoe UI", 10, FontStyle.Regular), Color.Black);
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Regular text
                AppendText(rtb, trimmed + "\n", new Font("Segoe UI", 10, FontStyle.Regular), Color.Black);
            }
        }

        // Scroll to top
        rtb.SelectionStart = 0;
        rtb.ScrollToCaret();
    }

    private static void AppendText(RichTextBox rtb, string text, Font font, Color color)
    {
        int start = rtb.TextLength;
        rtb.AppendText(text);
        rtb.Select(start, text.Length);
        rtb.SelectionFont = font;
        rtb.SelectionColor = color;
        rtb.SelectionLength = 0;
    }

    private static string LoadChangelog()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Youtube_Downloader.CHANGELOG.md";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return "# Changelog\n\nChangelog not found.";
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return $"# Changelog\n\nError loading changelog: {ex.Message}";
        }
    }
}
