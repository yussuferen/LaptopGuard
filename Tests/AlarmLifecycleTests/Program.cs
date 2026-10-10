using System.Reflection;
using LaptopGuard;
using LaptopGuard.Configuration;
using LaptopGuard.Sensors;
using LaptopGuard.Services;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CheckDisarmedSettings();
        CheckArmedSettingsAndRepeatedSave();
        CheckCountdownSettings();
        CheckRestartFailure();
        CheckHotkeyFailure();
        Console.WriteLine("Passed: 5 settings reload lifecycle scenarios.");
    }

    private static ConfigManager CreateConfig()
    {
        FakeSensor.Instances.Clear();
        FakeSensor.FailOnStart = null;
        HotkeyService.RegistrationSucceeds = true;
        var config = new ConfigManager();
        config.Config.ActiveSensors = ["PowerCableSensor"];
        config.Config.ArmCountdownSeconds = 1;
        return config;
    }

    private static void Arm(AlarmManager alarm)
    {
        alarm.Toggle();
        // Deliver a countdown tick directly so tests do not rely on wall-clock delays.
        typeof(AlarmManager).GetMethod("OnCountdownTick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(alarm, [null, EventArgs.Empty]);
        Require(alarm.State == AlarmState.Armed, "Alarm must be armed before saving.");
    }

    private static void CheckDisarmedSettings()
    {
        var config = CreateConfig();
        using var alarm = new AlarmManager(config);
        var oldSensor = FakeSensor.Instances.Single();
        config.Config.ActiveSensors = ["UsbSensor"];
        Require(alarm.ApplySettings(), "Hotkey must register.");
        Require(alarm.State == AlarmState.Disarmed, "Saving while disarmed must not enable protection.");
        Require(oldSensor.IsDisposed && !FakeSensor.Instances.Last().IsActive, "Inactive sensors must be replaced.");
        Require(!alarm.TelegramService.IsRunning && !SleepPreventionService.IsEnabled, "Disarmed services must stay off.");
    }

    private static void CheckArmedSettingsAndRepeatedSave()
    {
        var config = CreateConfig();
        using var alarm = new AlarmManager(config);
        Arm(alarm);
        var oldSensor = FakeSensor.Instances.Single();
        var states = new List<AlarmState>();
        int notifications = 0;
        alarm.StateChanged += states.Add;
        alarm.TriggerDetected += _ => notifications++;
        config.Config.ActiveSensors = ["UsbSensor"];
        config.Config.TelegramBotToken = "updated-test-token";
        config.Config.Hotkey = "Ctrl+Shift+L";

        Require(alarm.ApplySettings(), "Changed hotkey must register.");
        Require(states.SequenceEqual([AlarmState.Disarmed, AlarmState.Armed]), "UI must observe off then immediately on.");
        Require(oldSensor.IsDisposed && !oldSensor.IsActive, "Old sensor must be stopped and disposed.");
        Require(FakeSensor.Instances.Last().Name == "UsbSensor" && FakeSensor.Instances.Last().IsActive, "New selection must be active.");
        Require(alarm.TelegramService.IsRunning && alarm.TelegramService.LastToken == "updated-test-token", "Telegram must restart with new settings.");
        Require(SleepPreventionService.IsEnabled && HotkeyService.LastRegisteredHotkey == "Ctrl+Shift+L", "Power protection and new hotkey must be applied.");
        oldSensor.Emit();
        Require(notifications == 0, "Removed sensors must not retain trigger subscriptions.");
        FakeSensor.Instances.Last().Emit();
        Require(notifications == 1, "New sensor must deliver events.");

        var previousSensor = FakeSensor.Instances.Last();
        states.Clear();
        Require(alarm.ApplySettings(), "Repeated save must succeed.");
        Require(states.SequenceEqual([AlarmState.Disarmed, AlarmState.Armed]), "Repeated save must preserve state transitions.");
        previousSensor.Emit();
        FakeSensor.Instances.Last().Emit();
        Require(notifications == 2, "Repeated saves must not duplicate event handlers.");
    }

    private static void CheckCountdownSettings()
    {
        var config = CreateConfig();
        using var alarm = new AlarmManager(config);
        alarm.Toggle();
        int remaining = 0;
        alarm.CountdownTick += value => remaining = value;
        config.Config.ArmCountdownSeconds = 7;
        Require(alarm.ApplySettings(), "Countdown settings reload must succeed.");
        Require(alarm.State == AlarmState.Arming && remaining == 7, "An ongoing countdown must restart with the new duration.");
        Require(!FakeSensor.Instances.Last().IsActive, "Sensors must remain off until countdown completes.");
    }

    private static void CheckRestartFailure()
    {
        var config = CreateConfig();
        using var alarm = new AlarmManager(config);
        Arm(alarm);
        config.Config.ActiveSensors = ["UsbSensor"];
        FakeSensor.FailOnStart = "UsbSensor";
        bool failed = false;
        try { alarm.ApplySettings(); }
        catch (InvalidOperationException) { failed = true; }
        Require(failed && alarm.State == AlarmState.Disarmed, "Failed restart must report failure and leave the UI disarmed.");
        Require(!alarm.TelegramService.IsRunning && !SleepPreventionService.IsEnabled, "Failed restart must stop services.");
    }

    private static void CheckHotkeyFailure()
    {
        var config = CreateConfig();
        using var alarm = new AlarmManager(config);
        Arm(alarm);
        HotkeyService.RegistrationSucceeds = false;
        Require(!alarm.ApplySettings(), "Hotkey conflict must be reported to the caller.");
        Require(alarm.State == AlarmState.Armed && FakeSensor.Instances.Last().IsActive, "Hotkey warning must not silently disable existing protection.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
