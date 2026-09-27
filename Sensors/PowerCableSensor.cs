namespace LaptopGuard.Sensors;

public sealed class PowerCableSensor : ISensor
{
    private System.Windows.Forms.Timer? _pollTimer;
    private PowerLineStatus _lastStatus;

    public int PollIntervalMs { get; set; } = 1000;

    public string Name => "PowerCableSensor";
    public bool IsActive { get; private set; }

    public event Action<string>? TriggerDetected;

    public void Start()
    {
        if (IsActive) return;

        _lastStatus = SystemInformation.PowerStatus.PowerLineStatus;

        _pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _pollTimer.Tick += OnPollTick;
        _pollTimer.Start();

        IsActive = true;
    }

    public void Stop()
    {
        if (!IsActive) return;

        if (_pollTimer != null)
        {
            _pollTimer.Stop();
            _pollTimer.Tick -= OnPollTick;
            _pollTimer.Dispose();
            _pollTimer = null;
        }

        IsActive = false;
    }

    private void OnPollTick(object? sender, EventArgs e)
    {
        var currentStatus = SystemInformation.PowerStatus.PowerLineStatus;

        if (_lastStatus == PowerLineStatus.Online && currentStatus == PowerLineStatus.Offline)
        {
            TriggerDetected?.Invoke("Power cable was unplugged!");
        }

        _lastStatus = currentStatus;
    }

    public void Dispose()
    {
        Stop();
    }
}
