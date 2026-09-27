using LaptopGuard.Configuration;
using LaptopGuard.Services;
using LaptopGuard.UI;

namespace LaptopGuard;

public enum AlarmState
{
    Disarmed,
    Arming,
    Armed
}

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ConfigManager _configManager;
    private AlarmManager _alarmManager;
    private readonly BorderOverlayManager _borderOverlay;

    private readonly Icon _iconGray;
    private readonly Icon _iconGreen;
    private readonly Icon _iconRed;

    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _telegramItem;

    public TrayApplicationContext(ConfigManager configManager)
    {
        _configManager = configManager;

        _iconGray = LoadEmbeddedIcon("LaptopGuard.Resources.icon_gray.ico");
        _iconGreen = LoadEmbeddedIcon("LaptopGuard.Resources.icon_green.ico");
        _iconRed = LoadEmbeddedIcon("LaptopGuard.Resources.icon_red.ico");

        _statusItem = new ToolStripMenuItem("Status: Disarmed") { Enabled = false };
        _telegramItem = new ToolStripMenuItem("Telegram: Idle (starts when armed)") { Enabled = false };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusItem);
        contextMenu.Items.Add(_telegramItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("⚙️ Settings…", null, OnSettingsClick);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, OnExit);

        _trayIcon = new NotifyIcon
        {
            Icon = _iconGray,
            Text = "LaptopGuard — Disarmed",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _borderOverlay = new BorderOverlayManager();

        _alarmManager = new AlarmManager(configManager);
        _alarmManager.StateChanged += OnAlarmStateChanged;
        _alarmManager.CountdownTick += OnCountdownTick;
        _alarmManager.TriggerDetected += OnTriggerDetected;
        _alarmManager.TelegramService.ConnectionStatusChanged += OnTelegramConnectionChanged;

        if (!_alarmManager.Initialize())
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "LaptopGuard",
                $"Failed to register hotkey '{configManager.Config.Hotkey}'. " +
                "It may be in use by another application.",
                ToolTipIcon.Warning);
        }
        else
        {
            _trayIcon.ShowBalloonTip(
                2000,
                "LaptopGuard",
                $"Running. Press {configManager.Config.Hotkey} to arm/disarm.",
                ToolTipIcon.Info);
        }

        if (configManager.Config.StartWithWindows)
        {
            StartupService.SetStartup(true);
        }
    }

    private void OnAlarmStateChanged(AlarmState newState)
    {
        UpdateTrayIcon(newState);

        switch (newState)
        {
            case AlarmState.Armed:
                _borderOverlay.Show();
                break;

            case AlarmState.Arming:
            case AlarmState.Disarmed:
                _borderOverlay.Hide();
                break;
        }
    }

    private void OnCountdownTick(int remainingSeconds)
    {
        _statusItem.Text = $"Status: Arming… ({remainingSeconds}s)";
        _trayIcon.Text = $"LaptopGuard — Arming… ({remainingSeconds}s)";
    }

    private void OnTriggerDetected(string reason)
    {
        _trayIcon.ShowBalloonTip(
            5000,
            "🚨 LaptopGuard — Trigger Detected!",
            reason,
            ToolTipIcon.Error);

        _ = Task.Run(() => CaptureAndNotifyAsync(reason));
    }

    private async Task CaptureAndNotifyAsync(string reason)
    {
        var telegram = _alarmManager.TelegramService;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            using var photoStream = CameraService.CaptureFrameAsStream();

            if (photoStream != null)
            {
                string caption =
                    $"🚨 *ALARM TRIGGERED*\n" +
                    $"📋 *Reason:* {EscapeMarkdown(reason)}\n" +
                    $"🕐 *Time:* {timestamp}";

                await telegram.SendPhotoAsync(photoStream, caption);
            }
            else
            {
                string message =
                    $"🚨 *ALARM TRIGGERED*\n" +
                    $"📋 *Reason:* {EscapeMarkdown(reason)}\n" +
                    $"🕐 *Time:* {timestamp}\n" +
                    $"📷 _Camera not available_";

                await telegram.SendMessageAsync(message);
            }
        }
        catch
        {
            try
            {
                await telegram.SendMessageAsync(
                    $"🚨 ALARM TRIGGERED: {reason} ({timestamp})");
            }
            catch
            {
            }
        }
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("`", "\\`");
    }

    private void UpdateTrayIcon(AlarmState state)
    {
        (Icon icon, string tooltip, string statusText) = state switch
        {
            AlarmState.Disarmed => (_iconGray, "LaptopGuard — Disarmed", "Status: Disarmed"),
            AlarmState.Arming => (_iconGreen, "LaptopGuard — Arming…", "Status: Arming…"),
            AlarmState.Armed => (_iconRed, "LaptopGuard — Armed", "Status: Armed 🔴"),
            _ => (_iconGray, "LaptopGuard", "Status: Unknown")
        };

        _trayIcon.Icon = icon;
        _trayIcon.Text = tooltip;
        _statusItem.Text = statusText;

        if (state == AlarmState.Disarmed)
        {
            _telegramItem.Text = "Telegram: Idle (starts when armed)";
        }
    }

    private void OnTelegramConnectionChanged(bool connected, string message)
    {
        if (_trayIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _trayIcon.ContextMenuStrip.BeginInvoke(() => UpdateTelegramStatus(connected, message));
        }
        else
        {
            UpdateTelegramStatus(connected, message);
        }
    }

    private void UpdateTelegramStatus(bool connected, string message)
    {
        _telegramItem.Text = connected
            ? "Telegram: ✅ Connected"
            : $"Telegram: ❌ {message}";
    }

    private static Icon LoadEmbeddedIcon(string resourceName)
    {
        var assembly = typeof(TrayApplicationContext).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded resource '{resourceName}' not found. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        }

        return new Icon(stream);
    }

    private SettingsForm? _settingsForm;

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        if (_alarmManager.State != AlarmState.Disarmed)
        {
            _trayIcon.ShowBalloonTip(
                2000,
                "LaptopGuard",
                "Disarm the alarm before changing settings.",
                ToolTipIcon.Warning);
            return;
        }

        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }

        _settingsForm = new SettingsForm(_configManager);
        _settingsForm.SettingsSaved += OnSettingsSaved;
        _settingsForm.Show();
    }

    private void OnSettingsSaved()
    {
        _alarmManager.Dispose();

        _alarmManager = new AlarmManager(_configManager);
        _alarmManager.StateChanged += OnAlarmStateChanged;
        _alarmManager.CountdownTick += OnCountdownTick;
        _alarmManager.TriggerDetected += OnTriggerDetected;
        _alarmManager.TelegramService.ConnectionStatusChanged += OnTelegramConnectionChanged;

        if (!_alarmManager.Initialize())
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "LaptopGuard",
                $"Failed to register hotkey '{_configManager.Config.Hotkey}'.",
                ToolTipIcon.Warning);
        }
        else
        {
            _trayIcon.ShowBalloonTip(
                2000,
                "LaptopGuard",
                "Settings saved successfully.",
                ToolTipIcon.Info);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _alarmManager.Dispose();
        _borderOverlay.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _alarmManager.Dispose();
            _borderOverlay.Dispose();
            _trayIcon.Dispose();
            _iconGray.Dispose();
            _iconGreen.Dispose();
            _iconRed.Dispose();
        }

        base.Dispose(disposing);
    }
}
