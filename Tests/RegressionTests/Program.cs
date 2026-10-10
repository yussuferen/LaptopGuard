using System.Text.Json;
using LaptopGuard.Configuration;
using LaptopGuard.Sensors;

var cases = new (string Name, string? Id, string? Class, string? Service, bool Expected)[]
{
    ("USB storage unplug", @"USB\VID_1234&PID_5678\DISK", "USB", "USBSTOR", true),
    ("USB mouse unplug", @"USB\VID_1234&PID_5678\MOUSE", "HIDClass", "HidUsb", true),
    ("Wired USB headset unplug", @"USB\VID_1234&PID_5678&MI_00\AUDIO", "MEDIA", "usbaudio", true),
    ("USB composite device unplug", @"USB\VID_1234&PID_5678\COMPOSITE", "USB", "usbccgp", true),
    ("Case-insensitive USB identity", @"usb\vid_1234&pid_5678\DISK", "USB", "USBSTOR", true),
    ("USB device with missing optional metadata", @"USB\VID_1234&PID_5678\DEVICE", null, null, true),
    ("Bluetooth headset disconnect", @"BTHENUM\DEV_123456789ABC\HEADSET", "Bluetooth", "BthEnum", false),
    ("Bluetooth LE disconnect", @"BTHLEDEVICE\DEVICE\HEADSET", "Bluetooth", null, false),
    ("Bluetooth audio endpoint disappears", @"SWD\MMDEVAPI\{AUDIO-ENDPOINT}", "AudioEndpoint", null, false),
    ("Bluetooth HID child disappears", @"HID\{BLUETOOTH-DEVICE}\KEYBOARD", "HIDClass", "HidBth", false),
    ("USB Bluetooth radio disappears", @"USB\VID_1234&PID_5678\RADIO", "Bluetooth", null, false),
    ("Bluetooth USB driver with missing class", @"USB\VID_1234&PID_5678\RADIO", null, "BTHUSB", false),
    ("Case-insensitive Bluetooth metadata", @"USB\VID_1234&PID_5678\RADIO", "bluetooth", "bthusb", false),
    ("USB root hub disappears", @"USB\ROOT_HUB30\HUB", "USB", null, false),
    ("USB 2 hub disappears", @"USB\VID_1234&PID_5678\HUB", "USB", "USBHUB", false),
    ("USB 3 hub disappears", @"USB\VID_1234&PID_5678\HUB", "USB", "USBHUB3", false),
    ("Unknown device identity", null, null, null, false),
    ("Empty device identity", "", null, null, false),
    ("Non-USB device containing USB in its ID", @"SWD\USB\VIRTUAL", "MEDIA", null, false)
};

foreach (var test in cases)
{
    bool actual = UsbDeviceFilter.ShouldTrigger(test.Id, test.Class, test.Service);
    if (actual != test.Expected)
    {
        throw new InvalidOperationException($"{test.Name}: expected {test.Expected}, got {actual}.");
    }
}

// Old settings must still load, and the removed setting must not be written back.
const string legacyJson = """
    {
      "telegramBotToken": "test-token",
      "telegramChatId": "12345",
      "activeSensors": ["UsbSensor"],
      "silentMode": false,
      "hotkey": "Ctrl+Shift+L",
      "armCountdownSeconds": 5,
      "startWithWindows": true
    }
    """;
var config = JsonSerializer.Deserialize<AppConfig>(legacyJson)!;
if (config.TelegramBotToken != "test-token" || config.TelegramChatId != "12345" ||
    config.ActiveSensors.Count != 1 || config.ActiveSensors[0] != "UsbSensor" ||
    config.Hotkey != "Ctrl+Shift+L" || config.ArmCountdownSeconds != 5 || !config.StartWithWindows)
{
    throw new InvalidOperationException("Existing settings were not preserved when loading legacy configuration.");
}

using var savedConfig = JsonDocument.Parse(JsonSerializer.Serialize(config));
if (savedConfig.RootElement.TryGetProperty("silentMode", out _))
{
    throw new InvalidOperationException("Removed silent mode setting was serialized.");
}

Console.WriteLine($"Passed: {cases.Length} USB/Bluetooth cases and legacy configuration compatibility.");
