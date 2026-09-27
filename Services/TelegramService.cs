using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using LaptopGuard.Configuration;

namespace LaptopGuard.Services;

public class TelegramService : IDisposable
{
    private readonly ConfigManager _configManager;
    private TelegramBotClient? _botClient;
    private CancellationTokenSource? _pollingCts;

    public event Action<bool, string>? ConnectionStatusChanged;

    public bool IsRunning => _pollingCts is { IsCancellationRequested: false };

    public TelegramService(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    public bool Start()
    {
        string token = _configManager.Config.TelegramBotToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            ConnectionStatusChanged?.Invoke(false, "Telegram Bot Token is not configured.");
            return false;
        }

        Stop();

        _botClient = new TelegramBotClient(token);
        _pollingCts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        _ = _botClient.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _pollingCts.Token
        );

        ConnectionStatusChanged?.Invoke(true, "Telegram bot started.");
        return true;
    }

    public void Stop()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
        _botClient = null;
    }

    public async Task SendMessageAsync(string text)
    {
        if (_botClient == null) return;

        string chatId = _configManager.Config.TelegramChatId;
        if (string.IsNullOrWhiteSpace(chatId)) return;

        await _botClient.SendMessage(chatId, text);
    }

    public async Task SendPhotoAsync(Stream photoStream, string? caption = null)
    {
        if (_botClient == null) return;

        string chatId = _configManager.Config.TelegramChatId;
        if (string.IsNullOrWhiteSpace(chatId)) return;

        await _botClient.SendPhoto(
            chatId,
            InputFile.FromStream(photoStream, "capture.jpg"),
            caption: caption
        );
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message) return;

        string command = text.Trim().ToLowerInvariant();

        int atIndex = command.IndexOf('@');
        if (atIndex > 0) command = command[..atIndex];

        switch (command)
        {
            case "/status":
                await HandleStatusCommand(message.Chat.Id, ct);
                break;
        }
    }

    private Task HandlePollingErrorAsync(
        ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        ConnectionStatusChanged?.Invoke(false, $"Polling error: {exception.Message}");
        return Task.CompletedTask;
    }

    private async Task HandleStatusCommand(long chatId, CancellationToken ct)
    {
        string batteryInfo = GetBatteryInfo();
        string networkInfo = GetNetworkInfo();
        string uptime = GetUptimeInfo();

        string report =
            "🖥️ *LaptopGuard Status Report*\n" +
            "━━━━━━━━━━━━━━━━━━━━━━\n" +
            $"🟢 *Device:* Online\n" +
            $"⏱️ *Uptime:* {uptime}\n" +
            $"🔋 *Battery:* {batteryInfo}\n" +
            $"🌐 *Network:* {networkInfo}\n" +
            "━━━━━━━━━━━━━━━━━━━━━━";

        await _botClient!.SendMessage(chatId, report, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }

    private static string GetBatteryInfo()
    {
        try
        {
            var powerStatus = SystemInformation.PowerStatus;
            int percent = (int)(powerStatus.BatteryLifePercent * 100);

            string chargingStatus = powerStatus.PowerLineStatus switch
            {
                PowerLineStatus.Online => "⚡ Charging",
                PowerLineStatus.Offline => "🔌 On Battery",
                _ => "❓ Unknown"
            };

            string emoji = percent switch
            {
                > 75 => "🟢",
                > 25 => "🟡",
                _ => "🔴"
            };

            string remaining = powerStatus.BatteryLifeRemaining > 0
                ? $" ({TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining):hh\\:mm} remaining)"
                : "";

            return $"{emoji} {percent}% — {chargingStatus}{remaining}";
        }
        catch
        {
            return "❓ Unable to read battery info";
        }
    }

    private static string GetNetworkInfo()
    {
        try
        {
            bool isConnected = NetworkInterface.GetIsNetworkAvailable();

            if (!isConnected)
                return "🔴 Disconnected";

            var activeInterface = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(ni => ni.Speed)
                .FirstOrDefault();

            if (activeInterface == null)
                return "🟢 Connected";

            string type = activeInterface.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Ethernet => "Ethernet",
                _ => activeInterface.NetworkInterfaceType.ToString()
            };

            return $"🟢 Connected ({type}: {activeInterface.Name})";
        }
        catch
        {
            return "❓ Unable to read network info";
        }
    }

    private static string GetUptimeInfo()
    {
        try
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        }
        catch
        {
            return "Unknown";
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
