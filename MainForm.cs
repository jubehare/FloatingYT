using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.WinForms;

namespace FloatingYT;

public sealed class MainForm : Form
{
    private WebView2 _web = null!;
    private Panel _inputPanel = null!;
    private Panel _loadingOverlay = null!;
    private TextBox _urlBox = null!;
    private Button _playBtn = null!;
    private Button _cancelBtn = null!;
    private ProgressBar _progress = null!;
    private Label _statusLbl = null!;

    private bool _inVideo;

    private const string MainFont = "Segoe UI";

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Dark title bar on Win11

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int dark = 1;
        DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
    }

    private static readonly string BackJs = LoadBackJs();

    private static string LoadBackJs()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("FloatingYT.back.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public MainForm()
    {
        Text = "FloatingYT";
        Size = new Size(760, 480);
        MinimumSize = new Size(480, 340);
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 18);
        Font = new Font(MainFont, 9f);

        _web = new WebView2 { Dock = DockStyle.Fill, Visible = false };
        Controls.Add(_web);

        BuildInputPanel();
        BuildLoadingOverlay();

        Load += async (_, _) => await InitWebAsync();
    }

    private void BuildInputPanel()
    {
        _inputPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
        };

        stack.Controls.Add(new Label
        {
            Text = "FloatingYT",
            Font = new Font(MainFont, 20f, FontStyle.Bold),
            ForeColor = Color.Black,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14),
        });

        _urlBox = new TextBox
        {
            PlaceholderText = "Paste a YouTube URL here...",
            Font = new Font(MainFont, 10f),
            Width = 340,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 6, 0),
        };
        _urlBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Play(); }
        };

        _playBtn = new Button
        {
            Text = "Play",
            Size = new Size(80, _urlBox.PreferredHeight + 2),
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 0, 4, 0),
        };
        _playBtn.Click += (_, _) => Play();

        _cancelBtn = new Button
        {
            Text = "Cancel",
            Size = new Size(64, _urlBox.PreferredHeight + 2),
            FlatStyle = FlatStyle.System,
            Visible = false,
            Margin = new Padding(0),
        };
        _cancelBtn.Click += (_, _) => GoBack();

        var urlRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 8),
        };
        urlRow.Controls.AddRange(new Control[] { _urlBox, _playBtn, _cancelBtn });
        stack.Controls.Add(urlRow);

        _progress = new ProgressBar
        {
            Width = 470,
            Height = 4,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Visible = false,
            Margin = new Padding(0, 0, 0, 4),
        };
        stack.Controls.Add(_progress);

        _statusLbl = new Label
        {
            Text = "Initializing...",
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font(MainFont, 8.5f),
            Margin = new Padding(0),
        };
        stack.Controls.Add(_statusLbl);

        _inputPanel.Controls.Add(stack);

        void CenterStack()
        {
            stack.Location = new Point(
                Math.Max(0, (_inputPanel.Width - stack.Width) / 2),
                Math.Max(0, (_inputPanel.Height - stack.Height) / 2)
            );
        }
        _inputPanel.Resize += (_, _) => CenterStack();
        stack.SizeChanged += (_, _) => CenterStack();
        CenterStack();

        Controls.Add(_inputPanel);
    }

    private void BuildLoadingOverlay()
    {
        _loadingOverlay = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Visible = false,
        };

        var spinner = new Label
        {
            Text = "Loading...",
            ForeColor = Color.FromArgb(160, 160, 160),
            Font = new Font(MainFont, 11f),
            AutoSize = true,
        };

        _loadingOverlay.Controls.Add(spinner);
        _loadingOverlay.Resize += (_, _) =>
            spinner.Location = new Point(
                Math.Max(0, (_loadingOverlay.Width - spinner.Width) / 2),
                Math.Max(0, (_loadingOverlay.Height - spinner.Height) / 2)
            );

        Controls.Add(_loadingOverlay);
        _loadingOverlay.BringToFront();
    }

    private async Task InitWebAsync()
    {
        SetStatus("Initializing...");
        try
        {
            await _web.EnsureCoreWebView2Async();

            _web.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (_inVideo && e.Uri == "about:blank")
                    BeginInvoke(GoBack);
            };

            _web.NavigationCompleted += async (_, _) =>
            {
                if (!_inVideo) return;
                var src = _web.Source?.ToString() ?? "";
                if (string.IsNullOrEmpty(src) || src == "about:blank") return;

                await _web.CoreWebView2.ExecuteScriptAsync(BackJs);

                await Task.Delay(600);
                BeginInvoke(() => _loadingOverlay.Visible = false);
            };

            SetStatus("Paste a URL and click Play");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", error: true);
        }
    }

    private void Play()
    {
        var url = _urlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            SetStatus("Please paste a URL first.", error: true);
            return;
        }

        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        string[] allowed = ["youtube.com", "youtu.be", "bilibili.com", "twitch.tv"];
        if (!allowed.Any(url.Contains))
        {
            SetStatus("Unsupported URL.", error: true);
            return;
        }

        _inVideo = true;
        _playBtn.Enabled = false;
        _cancelBtn.Visible = true;
        _progress.Visible = true;
        _urlBox.Enabled = false;
        SetStatus("Loading video...");

        _inputPanel.Visible = false;
        _loadingOverlay.Visible = true;
        _loadingOverlay.BringToFront();
        _web.Visible = true;
        _web.CoreWebView2.Navigate(url);
    }

    private void GoBack()
    {
        _inVideo = false;

        _loadingOverlay.Visible = false;
        _web.Visible = false;
        _web.CoreWebView2.Navigate("about:blank");

        _inputPanel.Visible = true;
        _playBtn.Enabled = true;
        _cancelBtn.Visible = false;
        _progress.Visible = false;
        _urlBox.Enabled = true;
        SetStatus("Paste a URL and click Play");
        _urlBox.Focus();
    }

    private void SetStatus(string text, bool error = false)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text, error)); return; }
        _statusLbl.Text = text;
        _statusLbl.ForeColor = error ? Color.Firebrick : Color.Gray;
    }
}
