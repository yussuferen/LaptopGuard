using System.Runtime.InteropServices;

namespace LaptopGuard.Sensors;

public sealed class LidSensor : ISensor
{
    private const int WM_POWERBROADCAST = 0x0218;
    private const int PBT_POWERSETTINGCHANGE = 0x8013;

    private static readonly Guid GUID_LIDSWITCH_STATE_CHANGE =
        new("BA3E0F4D-B817-4094-A2D1-D56379E6A0F3");

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr RegisterPowerSettingNotification(
        IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;
        public uint DataLength;
    }

    private sealed class LidWindow : NativeWindow, IDisposable
    {
        public event Action<bool>? LidStateChanged;

        public LidWindow()
        {
            CreateHandle(new CreateParams
            {
                Caption = "LaptopGuard_LidSensor",
                Parent = new IntPtr(-3)
            });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_POWERBROADCAST && m.WParam.ToInt32() == PBT_POWERSETTINGCHANGE)
            {
                var setting = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(m.LParam);

                if (setting.PowerSetting == GUID_LIDSWITCH_STATE_CHANGE)
                {
                    int dataOffset = Marshal.SizeOf<POWERBROADCAST_SETTING>();
                    int lidState = Marshal.ReadInt32(m.LParam, dataOffset);

                    // 0 = closed, 1 = open
                    LidStateChanged?.Invoke(lidState == 0);
                }
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }

    private LidWindow? _window;
    private IntPtr _notificationHandle = IntPtr.Zero;

    public string Name => "LidSensor";
    public bool IsActive { get; private set; }

    public event Action<string>? TriggerDetected;

    public void Start()
    {
        if (IsActive) return;

        _window = new LidWindow();
        _window.LidStateChanged += OnLidStateChanged;

        var guid = GUID_LIDSWITCH_STATE_CHANGE;
        _notificationHandle = RegisterPowerSettingNotification(_window.Handle, ref guid, 0);

        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive) return;

        if (_notificationHandle != IntPtr.Zero)
        {
            UnregisterPowerSettingNotification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;
        }

        _window?.Dispose();
        _window = null;

        IsActive = false;
    }

    private void OnLidStateChanged(bool isClosed)
    {
        if (isClosed && IsActive)
        {
            TriggerDetected?.Invoke("Laptop lid was closed!");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
