# LaptopGuard 

A Windows tray app I built for my own use: when I step away from my laptop in a library or café, it watches for tampering (lid close, power unplug, USB removal) and alerts my phone via Telegram.

---

## Features

- **Runs in the background** — lives in the system tray, no console window, no taskbar clutter.
- **Minimal idle footprint** — sensors, Telegram polling, and camera access are fully off while disarmed.
- **Global hotkey** — arm/disarm with a customizable shortcut (default `Ctrl+Alt+L`).
- **Tamper sensors** — detects lid close, power cable disconnection, and USB device removal.
- **Tamper alerts** — captures a webcam photo and sends it to Telegram with a timestamp and reason.
- **Autostart support** — optionally launch with Windows, toggleable from Settings.

---

## Requirements

- Windows 11 or Windows 10
- .NET 10.0 Runtime / SDK

---

## Install & Run

```bash
git clone https://github.com/yussuferen/LaptopGuard.git
cd LaptopGuard
dotnet build
dotnet run
```

---

## Setting Up Telegram

1. **Create a bot:** Open Telegram and message [@BotFather](https://t.me/botfather). Send `/newbot` and follow the prompts. BotFather will reply with a **Bot Token** — copy it.
2. **Get your Chat ID:** Message [@userinfobot](https://t.me/userinfobot) on Telegram — it will reply with your **Chat ID**.
3. **Start a conversation with your bot:** Search for your bot's username on Telegram and click start (`/start`). This is required — Telegram bots can't message users who haven't messaged them first.
4. Paste both values into LaptopGuard's Settings menu (or `config.json`) as described below.

---

## Configuration

### Method 1: Using the GUI
1. Launch the application.
2. Right-click the shield icon in the system tray.
3. Select **⚙️ Settings…**.
4. Enter your Telegram Bot Token and Chat ID, toggle desired sensors, and click **Save**.

### Method 2: Manual JSON Configuration
1. Copy `config.example.json` and name it `config.json` in the root directory:
   ```bash
   cp config.example.json config.json
   ```
2. Fill in your credentials:
   ```json
   {
     "telegramBotToken": "YOUR_TELEGRAM_BOT_TOKEN_HERE",
     "telegramChatId": "YOUR_TELEGRAM_CHAT_ID_HERE",
     "activeSensors": [
       "LidSensor",
       "PowerCableSensor",
       "UsbSensor"
     ],
     "silentMode": true,
     "hotkey": "Ctrl+Alt+L",
     "armCountdownSeconds": 3,
     "startWithWindows": false
   }
   ```
---

## Limitations

- **Needs an internet connection.** Alerts are sent over Telegram, so if the laptop leaves the network it was connected to (e.g. the library Wi-Fi), no message can be sent. Alerts only go out while the device is still online, which is usually the moment of the trigger itself (unplugging, closing the lid).
- **Deterrent, not protection.** Someone who shuts down or wipes the machine can bypass it.
