using System.Runtime.InteropServices;

namespace LaptopGuard.Services;

public static class SleepPreventionService
{
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_AWAYMODE_REQUIRED = 0x00000040;
    private const uint ES_CONTINUOUS = 0x80000000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroup,
        ref Guid setting,
        out uint acValueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroup,
        ref Guid setting,
        out uint dcValueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroup,
        ref Guid setting,
        uint acValueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subGroup,
        ref Guid setting,
        uint dcValueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

    private static readonly Guid SubGroupButtons = new("4f971e89-eebd-4455-a8de-9e59040e7347");
    private static readonly Guid SettingLidAction = new("5ca83367-6e45-459f-a27b-476b1d01c936");
    private static readonly string StateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".power_recovery");

    private static bool _isEnabled;
    private static uint? _originalAcLidAction;
    private static uint? _originalDcLidAction;
    private static Guid _activeSchemeGuid;

    static SleepPreventionService()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Disable();
        RestoreIfRecovering();
    }

    public static void RestoreIfRecovering()
    {
        try
        {
            if (File.Exists(StateFilePath))
            {
                string content = File.ReadAllText(StateFilePath).Trim();
                string[] parts = content.Split(',');
                if (parts.Length == 2 && uint.TryParse(parts[0], out uint ac) && uint.TryParse(parts[1], out uint dc))
                {
                    if (PowerGetActiveScheme(IntPtr.Zero, out IntPtr pGuid) == 0 && pGuid != IntPtr.Zero)
                    {
                        var scheme = Marshal.PtrToStructure<Guid>(pGuid);
                        var subGroup = SubGroupButtons;
                        var setting = SettingLidAction;

                        PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref subGroup, ref setting, ac);
                        PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref subGroup, ref setting, dc);
                        PowerSetActiveScheme(IntPtr.Zero, ref scheme);
                    }
                }

                File.Delete(StateFilePath);
            }
        }
        catch
        {
        }
    }

    public static void Enable()
    {
        if (_isEnabled) return;

        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);

        try
        {
            if (PowerGetActiveScheme(IntPtr.Zero, out IntPtr pGuid) == 0 && pGuid != IntPtr.Zero)
            {
                _activeSchemeGuid = Marshal.PtrToStructure<Guid>(pGuid);
                var subGroup = SubGroupButtons;
                var setting = SettingLidAction;

                if (PowerReadACValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, out uint ac) == 0)
                {
                    _originalAcLidAction = ac;
                }

                if (PowerReadDCValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, out uint dc) == 0)
                {
                    _originalDcLidAction = dc;
                }

                if (_originalAcLidAction.HasValue && _originalDcLidAction.HasValue)
                {
                    File.WriteAllText(StateFilePath, $"{_originalAcLidAction.Value},{_originalDcLidAction.Value}");
                }

                // 0 = Do nothing
                PowerWriteACValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, 0);
                PowerWriteDCValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, 0);
                PowerSetActiveScheme(IntPtr.Zero, ref _activeSchemeGuid);
            }
        }
        catch
        {
        }

        _isEnabled = true;
    }

    public static void Disable()
    {
        if (!_isEnabled) return;

        try
        {
            if (_originalAcLidAction.HasValue || _originalDcLidAction.HasValue)
            {
                var subGroup = SubGroupButtons;
                var setting = SettingLidAction;

                if (_originalAcLidAction.HasValue)
                {
                    PowerWriteACValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, _originalAcLidAction.Value);
                }

                if (_originalDcLidAction.HasValue)
                {
                    PowerWriteDCValueIndex(IntPtr.Zero, ref _activeSchemeGuid, ref subGroup, ref setting, _originalDcLidAction.Value);
                }

                PowerSetActiveScheme(IntPtr.Zero, ref _activeSchemeGuid);
            }

            if (File.Exists(StateFilePath))
            {
                File.Delete(StateFilePath);
            }
        }
        catch
        {
        }
        finally
        {
            SetThreadExecutionState(ES_CONTINUOUS);
            _isEnabled = false;
        }
    }
}
