using LaptopGuard.Configuration;
using LaptopGuard.Services;

namespace LaptopGuard.UI;

public sealed class SettingsForm : Form
{
    private readonly ConfigManager _configManager;

    private readonly TextBox _txtBotToken;
    private readonly TextBox _txtChatId;

    private readonly CheckBox _chkLidSensor;
    private readonly CheckBox _chkPowerCableSensor;
    private readonly CheckBox _chkUsbSensor;

    private readonly TextBox _txtHotkey;
    private readonly NumericUpDown _nudCountdown;
    private readonly CheckBox _chkSilentMode;
    private readonly CheckBox _chkStartWithWindows;

    private readonly Button _btnSave;
    private readonly Button _btnCancel;

    public event Action? SettingsSaved;

    public SettingsForm(ConfigManager configManager)
    {
        _configManager = configManager;

        Text = "LaptopGuard — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9.5f);

        int pad = 14;
        int innerPad = 12;
        int rowH = 32;
        int labelW = 105;
        int groupW = 430;
        int formW = groupW + pad * 2 + 2;

        int y = pad;

        var grpTelegram = new GroupBox
        {
            Text = "Telegram",
            Location = new Point(pad, y),
            Size = new Size(groupW, 20 + rowH * 2 + innerPad)
        };

        int fieldW = groupW - labelW - innerPad * 2 - 4;
        int gy = 24;

        grpTelegram.Controls.Add(new Label
        {
            Text = "Bot Token:",
            Location = new Point(innerPad, gy + 3),
            AutoSize = true
        });
        _txtBotToken = new TextBox
        {
            Location = new Point(innerPad + labelW, gy),
            Size = new Size(fieldW, 24),
            PlaceholderText = "123456:ABC-DEF..."
        };
        grpTelegram.Controls.Add(_txtBotToken);

        gy += rowH;

        grpTelegram.Controls.Add(new Label
        {
            Text = "Chat ID:",
            Location = new Point(innerPad, gy + 3),
            AutoSize = true
        });
        _txtChatId = new TextBox
        {
            Location = new Point(innerPad + labelW, gy),
            Size = new Size(fieldW, 24),
            PlaceholderText = "123456789"
        };
        grpTelegram.Controls.Add(_txtChatId);

        Controls.Add(grpTelegram);
        y += grpTelegram.Height + pad;

        var grpSensors = new GroupBox
        {
            Text = "Active Sensors",
            Location = new Point(pad, y),
            Size = new Size(groupW, 20 + rowH * 3 + innerPad)
        };

        gy = 24;

        _chkLidSensor = new CheckBox
        {
            Text = "Lid close detection",
            Location = new Point(innerPad, gy),
            AutoSize = true
        };
        grpSensors.Controls.Add(_chkLidSensor);

        gy += rowH;

        _chkPowerCableSensor = new CheckBox
        {
            Text = "Power cable unplug detection",
            Location = new Point(innerPad, gy),
            AutoSize = true
        };
        grpSensors.Controls.Add(_chkPowerCableSensor);

        gy += rowH;

        _chkUsbSensor = new CheckBox
        {
            Text = "USB device removal detection",
            Location = new Point(innerPad, gy),
            AutoSize = true
        };
        grpSensors.Controls.Add(_chkUsbSensor);

        Controls.Add(grpSensors);
        y += grpSensors.Height + pad;

        var grpAlarm = new GroupBox
        {
            Text = "Alarm & System",
            Location = new Point(pad, y),
            Size = new Size(groupW, 20 + rowH * 4 + innerPad)
        };

        gy = 24;

        grpAlarm.Controls.Add(new Label
        {
            Text = "Hotkey:",
            Location = new Point(innerPad, gy + 3),
            AutoSize = true
        });
        _txtHotkey = new TextBox
        {
            Location = new Point(innerPad + labelW, gy),
            Size = new Size(180, 24),
            PlaceholderText = "Ctrl+Alt+L"
        };
        grpAlarm.Controls.Add(_txtHotkey);

        gy += rowH;

        grpAlarm.Controls.Add(new Label
        {
            Text = "Countdown (s):",
            Location = new Point(innerPad, gy + 3),
            AutoSize = true
        });
        _nudCountdown = new NumericUpDown
        {
            Location = new Point(innerPad + labelW, gy),
            Size = new Size(70, 24),
            Minimum = 1,
            Maximum = 30,
            Value = 3
        };
        grpAlarm.Controls.Add(_nudCountdown);

        gy += rowH;

        _chkSilentMode = new CheckBox
        {
            Text = "Silent mode (no local sound)",
            Location = new Point(innerPad, gy),
            AutoSize = true
        };
        grpAlarm.Controls.Add(_chkSilentMode);

        gy += rowH;

        _chkStartWithWindows = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(innerPad, gy),
            AutoSize = true
        };
        grpAlarm.Controls.Add(_chkStartWithWindows);

        Controls.Add(grpAlarm);
        y += grpAlarm.Height + pad;

        int btnW = 90;
        int btnH = 34;

        _btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(btnW, btnH),
            Location = new Point(formW - pad - btnW - 2, y),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_btnCancel);

        _btnSave = new Button
        {
            Text = "Save",
            Size = new Size(btnW, btnH),
            Location = new Point(_btnCancel.Left - btnW - 8, y),
            DialogResult = DialogResult.OK
        };
        _btnSave.Click += OnSaveClick;
        Controls.Add(_btnSave);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        y += btnH + pad;

        ClientSize = new Size(formW, y);

        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        var cfg = _configManager.Config;

        _txtBotToken.Text = cfg.TelegramBotToken;
        _txtChatId.Text = cfg.TelegramChatId;

        _chkLidSensor.Checked = cfg.ActiveSensors.Contains("LidSensor");
        _chkPowerCableSensor.Checked = cfg.ActiveSensors.Contains("PowerCableSensor");
        _chkUsbSensor.Checked = cfg.ActiveSensors.Contains("UsbSensor");

        _txtHotkey.Text = cfg.Hotkey;
        _nudCountdown.Value = Math.Clamp(cfg.ArmCountdownSeconds, 1, 30);
        _chkSilentMode.Checked = cfg.SilentMode;

        _chkStartWithWindows.Checked = StartupService.IsStartupEnabled();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        var cfg = _configManager.Config;

        cfg.TelegramBotToken = _txtBotToken.Text.Trim();
        cfg.TelegramChatId = _txtChatId.Text.Trim();

        cfg.ActiveSensors.Clear();
        if (_chkLidSensor.Checked) cfg.ActiveSensors.Add("LidSensor");
        if (_chkPowerCableSensor.Checked) cfg.ActiveSensors.Add("PowerCableSensor");
        if (_chkUsbSensor.Checked) cfg.ActiveSensors.Add("UsbSensor");

        cfg.Hotkey = _txtHotkey.Text.Trim();
        cfg.ArmCountdownSeconds = (int)_nudCountdown.Value;
        cfg.SilentMode = _chkSilentMode.Checked;

        bool startWithWindows = _chkStartWithWindows.Checked;
        cfg.StartWithWindows = startWithWindows;
        StartupService.SetStartup(startWithWindows);

        _configManager.Save();
        SettingsSaved?.Invoke();

        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
    }
}
