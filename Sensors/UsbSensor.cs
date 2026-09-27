using System.Management;

namespace LaptopGuard.Sensors;

public sealed class UsbSensor : ISensor
{
    private ManagementEventWatcher? _watcher;

    public string Name => "UsbSensor";
    public bool IsActive { get; private set; }

    public event Action<string>? TriggerDetected;

    public void Start()
    {
        if (IsActive) return;

        var query = new WqlEventQuery(
            "__InstanceDeletionEvent",
            TimeSpan.FromSeconds(2),
            "TargetInstance ISA 'Win32_USBControllerDevice'");

        _watcher = new ManagementEventWatcher(query);
        _watcher.EventArrived += OnUsbDeviceRemoved;
        _watcher.Start();

        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive) return;

        if (_watcher != null)
        {
            _watcher.Stop();
            _watcher.EventArrived -= OnUsbDeviceRemoved;
            _watcher.Dispose();
            _watcher = null;
        }

        IsActive = false;
    }

    private void OnUsbDeviceRemoved(object sender, EventArrivedEventArgs e)
    {
        if (!IsActive) return;

        string deviceInfo = "unknown device";

        try
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string? dependent = targetInstance["Dependent"]?.ToString();

            if (!string.IsNullOrEmpty(dependent))
            {
                int idStart = dependent.IndexOf("DeviceID=\"", StringComparison.OrdinalIgnoreCase);
                if (idStart >= 0)
                {
                    idStart += "DeviceID=\"".Length;
                    int idEnd = dependent.IndexOf('"', idStart);
                    if (idEnd > idStart)
                    {
                        deviceInfo = dependent[idStart..idEnd];
                    }
                }
            }
        }
        catch
        {
        }

        TriggerDetected?.Invoke($"USB device removed: {deviceInfo}");
    }

    public void Dispose()
    {
        Stop();
    }
}
