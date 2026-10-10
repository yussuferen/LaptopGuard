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
            @"TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.DeviceID LIKE 'USB\\%'");

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

        try
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string? deviceId = targetInstance["DeviceID"]?.ToString();
            string? deviceClass = targetInstance["PNPClass"]?.ToString();
            string? service = targetInstance["Service"]?.ToString();

            if (!UsbDeviceFilter.ShouldTrigger(deviceId, deviceClass, service)) return;

            TriggerDetected?.Invoke($"USB device removed: {deviceId}");
        }
        catch (ManagementException)
        {
            // An unreadable event is not evidence that a physical USB device was removed.
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
