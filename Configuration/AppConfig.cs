using System.Text.Json.Serialization;

namespace LaptopGuard.Configuration;

public class AppConfig
{
    [JsonPropertyName("telegramBotToken")]
    public string TelegramBotToken { get; set; } = string.Empty;

    [JsonPropertyName("telegramChatId")]
    public string TelegramChatId { get; set; } = string.Empty;

    [JsonPropertyName("activeSensors")]
    public List<string> ActiveSensors { get; set; } = ["LidSensor", "PowerCableSensor", "UsbSensor"];

    [JsonPropertyName("silentMode")]
    public bool SilentMode { get; set; } = true;

    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = "Ctrl+Alt+L";

    [JsonPropertyName("armCountdownSeconds")]
    public int ArmCountdownSeconds { get; set; } = 3;

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;
}
