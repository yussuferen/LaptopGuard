using System.Runtime.InteropServices;

namespace LaptopGuard.UI;

public sealed class BorderOverlayForm : Form
{
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const int LWA_ALPHA = 0x00000002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    public BorderOverlayForm(Rectangle bounds, Color borderColor)
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = borderColor;
        Bounds = bounds;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW
                        | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // WS_EX_LAYERED windows start fully transparent without explicitly setting alpha.
        SetLayeredWindowAttributes(Handle, 0, 255, LWA_ALPHA);
    }

    protected override bool ShowWithoutActivation => true;
}

public sealed class BorderOverlayManager : IDisposable
{
    private readonly List<BorderOverlayForm> _overlays = [];
    private bool _visible;

    public int Thickness { get; set; } = 5;
    public Color BorderColor { get; set; } = Color.FromArgb(220, 38, 38);

    public void Show()
    {
        if (_visible) return;

        var screens = new[] { Screen.PrimaryScreen! };

        foreach (var screen in screens)
        {
            CreateBorderForms(screen.Bounds);
        }

        _visible = true;
    }

    public void Hide()
    {
        if (!_visible) return;

        foreach (var overlay in _overlays)
        {
            overlay.Close();
            overlay.Dispose();
        }

        _overlays.Clear();
        _visible = false;
    }

    private void CreateBorderForms(Rectangle screenBounds)
    {
        int t = Thickness;
        int x = screenBounds.X;
        int y = screenBounds.Y;
        int w = screenBounds.Width;
        int h = screenBounds.Height;

        AddOverlay(new Rectangle(x, y, w, t));
        AddOverlay(new Rectangle(x, y + h - t, w, t));
        AddOverlay(new Rectangle(x, y + t, t, h - 2 * t));
        AddOverlay(new Rectangle(x + w - t, y + t, t, h - 2 * t));
    }

    private void AddOverlay(Rectangle bounds)
    {
        var form = new BorderOverlayForm(bounds, BorderColor);
        form.Show();
        _overlays.Add(form);
    }

    public void Dispose()
    {
        Hide();
    }
}
