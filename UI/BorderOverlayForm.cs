using System.Runtime.InteropServices;

namespace LaptopGuard.UI;

public sealed class BorderOverlayForm : Form
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    public BorderOverlayForm(Rectangle bounds, Color borderColor, int thickness)
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = borderColor;
        Bounds = bounds;

        var outer = new Rectangle(0, 0, bounds.Width, bounds.Height);
        var inner = new Rectangle(thickness, thickness, bounds.Width - 2 * thickness, bounds.Height - 2 * thickness);
        var region = new Region(outer);
        region.Exclude(inner);
        Region = region;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
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

        foreach (var screen in Screen.AllScreens)
        {
            var form = new BorderOverlayForm(screen.Bounds, BorderColor, Thickness);
            form.Show();
            _overlays.Add(form);
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

    public void Dispose()
    {
        Hide();
    }
}
