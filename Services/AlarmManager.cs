using LaptopGuard.Configuration;
using LaptopGuard.Sensors;

namespace LaptopGuard.Services;

public sealed class AlarmManager : IDisposable
{
    private readonly ConfigManager _configManager;
    private readonly TelegramService _telegramService;
    private readonly HotkeyService _hotkeyService;
    private readonly List<ISensor> _sensors = [];

    private System.Windows.Forms.Timer? _countdownTimer;
    private int _countdownRemaining;

    private AlarmState _state = AlarmState.Disarmed;

    public event Action<AlarmState>? StateChanged;
    public event Action<int>? CountdownTick;
    public event Action<string>? TriggerDetected;

    public AlarmState State => _state;
    public TelegramService TelegramService => _telegramService;

    public AlarmManager(ConfigManager configManager)
    {
        _configManager = configManager;

        _telegramService = new TelegramService(configManager);
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        BuildSensors();
    }

    public bool Initialize()
    {
        string hotkey = _configManager.Config.Hotkey;
        bool registered = _hotkeyService.Register(hotkey);
        return registered;
    }

    public void Toggle()
    {
        switch (_state)
        {
            case AlarmState.Disarmed:
                StartArming();
                break;
            case AlarmState.Arming:
            case AlarmState.Armed:
                Disarm();
                break;
        }
    }

    private void StartArming()
    {
        int countdownSeconds = _configManager.Config.ArmCountdownSeconds;

        SetState(AlarmState.Arming);

        _countdownRemaining = countdownSeconds;
        CountdownTick?.Invoke(_countdownRemaining);

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();
    }

    private void Arm()
    {
        StopCountdown();
        SetState(AlarmState.Armed);

        _telegramService.Start();
        StartSensors();
    }

    private void Disarm()
    {
        StopCountdown();
        StopSensors();
        _telegramService.Stop();
        SetState(AlarmState.Disarmed);
    }

    private void BuildSensors()
    {
        var activeSensorNames = _configManager.Config.ActiveSensors;

        foreach (string name in activeSensorNames)
        {
            ISensor? sensor = name switch
            {
                "LidSensor" => new LidSensor(),
                "PowerCableSensor" => new PowerCableSensor(),
                "UsbSensor" => new UsbSensor(),
                _ => null
            };

            if (sensor != null)
            {
                _sensors.Add(sensor);
            }
        }
    }

    private void StartSensors()
    {
        foreach (var sensor in _sensors)
        {
            sensor.TriggerDetected += OnSensorTriggered;
            sensor.Start();
        }
    }

    private void StopSensors()
    {
        foreach (var sensor in _sensors)
        {
            sensor.Stop();
            sensor.TriggerDetected -= OnSensorTriggered;
        }
    }

    private void OnSensorTriggered(string reason)
    {
        if (_state != AlarmState.Armed) return;

        var form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

        if (form is { InvokeRequired: true })
        {
            form.BeginInvoke(() => TriggerDetected?.Invoke(reason));
        }
        else
        {
            TriggerDetected?.Invoke(reason);
        }
    }

    private void OnHotkeyPressed()
    {
        Toggle();
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        _countdownRemaining--;

        if (_countdownRemaining <= 0)
        {
            Arm();
        }
        else
        {
            CountdownTick?.Invoke(_countdownRemaining);
        }
    }

    private void StopCountdown()
    {
        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Tick -= OnCountdownTick;
            _countdownTimer.Dispose();
            _countdownTimer = null;
        }
    }

    private void SetState(AlarmState newState)
    {
        if (_state == newState) return;
        _state = newState;
        StateChanged?.Invoke(newState);
    }

    public void Dispose()
    {
        StopCountdown();
        StopSensors();

        foreach (var sensor in _sensors)
        {
            sensor.Dispose();
        }

        _sensors.Clear();
        _telegramService.Dispose();
        _hotkeyService.Dispose();
    }
}
