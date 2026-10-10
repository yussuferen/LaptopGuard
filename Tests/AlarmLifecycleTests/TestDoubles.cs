// Exercise the real AlarmManager without changing Windows power settings,
// registering global hotkeys, accessing hardware, or contacting Telegram.
namespace LaptopGuard
{
    public enum AlarmState { Disarmed, Arming, Armed }
}

namespace LaptopGuard.Configuration
{
    public sealed class ConfigManager
    {
        public AppConfig Config { get; } = new();
    }
}

namespace LaptopGuard.Services
{
    public sealed class TelegramService(Configuration.ConfigManager config) : IDisposable
    {
        public bool IsRunning { get; private set; }
        public string LastToken { get; private set; } = "";
        public bool Start()
        {
            LastToken = config.Config.TelegramBotToken;
            IsRunning = true;
            return true;
        }
        public void Stop() => IsRunning = false;
        public void Dispose() => Stop();
    }

    public sealed class HotkeyService : IDisposable
    {
        public static string LastRegisteredHotkey { get; private set; } = "";
        public static bool RegistrationSucceeds { get; set; } = true;
        public event Action? HotkeyPressed;
        public bool Register(string hotkey)
        {
            LastRegisteredHotkey = hotkey;
            return RegistrationSucceeds;
        }
        public void Press() => HotkeyPressed?.Invoke();
        public void Dispose() { }
    }

    public static class SleepPreventionService
    {
        public static bool IsEnabled { get; private set; }
        public static void Enable() => IsEnabled = true;
        public static void Disable() => IsEnabled = false;
    }
}

namespace LaptopGuard.Sensors
{
    public abstract class FakeSensor(string name) : ISensor
    {
        public static List<FakeSensor> Instances { get; } = [];
        public static string? FailOnStart { get; set; }
        public string Name => name;
        public bool IsActive { get; private set; }
        public bool IsDisposed { get; private set; }
        public event Action<string>? TriggerDetected;

        public void Start()
        {
            if (FailOnStart == Name) throw new InvalidOperationException("Simulated sensor failure.");
            IsActive = true;
        }
        public void Stop() => IsActive = false;
        public void Emit() => TriggerDetected?.Invoke(Name);
        public void Dispose()
        {
            Stop();
            IsDisposed = true;
        }
    }

    public sealed class LidSensor : FakeSensor
    {
        public LidSensor() : base("LidSensor") => Instances.Add(this);
    }
    public sealed class PowerCableSensor : FakeSensor
    {
        public PowerCableSensor() : base("PowerCableSensor") => Instances.Add(this);
    }
    public sealed class UsbSensor : FakeSensor
    {
        public UsbSensor() : base("UsbSensor") => Instances.Add(this);
    }
}
