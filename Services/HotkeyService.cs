using System.Runtime.InteropServices;

namespace LaptopGuard.Services;

public sealed class HotkeyService : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    [Flags]
    private enum KeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public event Action? HotkeyReceived;

        public HotkeyWindow()
        {
            var cp = new CreateParams
            {
                Caption = "LaptopGuard_HotkeyListener",
                Parent = new IntPtr(-3) // HWND_MESSAGE
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyReceived?.Invoke();
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }

    private HotkeyWindow? _window;
    private bool _registered;

    public event Action? HotkeyPressed;

    public bool Register(string hotkeyString)
    {
        Unregister();

        if (!TryParseHotkey(hotkeyString, out uint modifiers, out uint vk))
        {
            return false;
        }

        _window = new HotkeyWindow();
        _window.HotkeyReceived += () => HotkeyPressed?.Invoke();

        _registered = RegisterHotKey(_window.Handle, HOTKEY_ID, modifiers, vk);

        if (!_registered)
        {
            _window.Dispose();
            _window = null;
        }

        return _registered;
    }

    public void Unregister()
    {
        if (_registered && _window != null)
        {
            UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _registered = false;
        }

        _window?.Dispose();
        _window = null;
    }

    public void Dispose()
    {
        Unregister();
    }

    private static bool TryParseHotkey(string hotkey, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(hotkey)) return false;

        string[] parts = hotkey.Split('+', StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= (uint)KeyModifiers.Ctrl;
                    break;
                case "ALT":
                    modifiers |= (uint)KeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= (uint)KeyModifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= (uint)KeyModifiers.Win;
                    break;
                default:
                    if (Enum.TryParse<Keys>(part, ignoreCase: true, out var key))
                    {
                        vk = (uint)key;
                    }
                    else if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                    {
                        vk = (uint)char.ToUpperInvariant(part[0]);
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }

        return vk != 0;
    }
}
