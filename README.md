# FixWidget

A lightweight WPF application for displaying [donatex.gg](https://donatex.gg) donation widgets as desktop overlays using WebView2 and ntfy notifications.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## 🎯 Features

- **donatex.gg Integration** - Display donation alerts and custom widgets from donatex.gg
- **Real-time notifications** - Instant widget display via ntfy pub-sub service
- **Customizable positioning** - Precise widget placement with resolution scaling
- **Transparent overlays** - Full transparency support with click-through option
- **Audio/Video support** - Autoplay enabled with volume control for alerts
- **Performance optimized** - Automatic cache cleanup and resource management
- **Zero configuration** - Works out of the box with simple JSON config

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- .NET 8.0 Runtime
- WebView2 Runtime (usually pre-installed on Windows 11)

### Installation

1. Download the latest release from [Releases](../../releases)
2. Extract the archive
3. Edit `cfgwidget.json` with your ntfy topic:
```json
{
  "NtfyUrl": "https://ntfy.sh/your-topic-here"
}
```
4. Run `FixWidget.exe`

### Usage

The application listens for messages from your ntfy topic. When donatex.gg sends a notification, it will contain a message in this format:

**Important**: The message arrives wrapped in an ntfy envelope. The actual widget data is in the `message` field as a JSON string.

#### ntfy Message Format

```json
{
  "id": "messageId",
  "time": 1234567890,
  "event": "message",
  "topic": "your-topic",
  "message": "[{\"id\":100000,\"steamid64\":\"-1\",\"login\":\"Empty\",\"text\":\"Empty\",\"createdAt\":1760036253509,\"extra\":{\"volume\":0.75,\"setupResolutionX\":2560.0,\"setupResolutionY\":1440.0,\"setupX\":500.0,\"setupY\":500.0,\"scale\":1.0,\"opacity\":1.0,\"durationMs\":10000.0,\"clickThrough\":true,\"url\":\"https://donatex.gg/widgets/custom-ai-widget/YOUR-WIDGET-ID\"}}]"
}
```

The `message` field contains a JSON array with the widget configuration. FixWidget automatically extracts and parses this inner JSON.

#### Manual Test Message

To test the widget manually, send this to your ntfy topic:

```bash
curl -d '[{
  "id": 100000,
  "steamid64": "-1",
  "login": "Empty", 
  "text": "Empty",
  "createdAt": 1760036253509,
  "extra": {
    "url": "https://donatex.gg/widgets/custom-ai-widget/YOUR-WIDGET-ID",
    "setupX": 500,
    "setupY": 500,
    "setupResolutionX": 2560,
    "setupResolutionY": 1440,
    "scale": 1.0,
    "opacity": 1.0,
    "volume": 0.75,
    "clickThrough": true,
    "durationMs": 10000
  }
}]' https://ntfy.sh/your-topic-here
```

The widget will appear instantly on your desktop!

## 📋 Message Format

### Message Structure

FixWidget expects messages in the following format (sent as JSON array):

```json
[{
  "id": 100000,
  "steamid64": "-1",
  "login": "Empty",
  "text": "Empty",
  "createdAt": 1760036253509,
  "extra": {
    "url": "https://donatex.gg/widgets/custom-ai-widget/YOUR-WIDGET-ID",
    "setupX": 500,
    "setupY": 500,
    "setupResolutionX": 2560,
    "setupResolutionY": 1440,
    "scale": 1.0,
    "opacity": 1.0,
    "volume": 0.75,
    "clickThrough": true,
    "durationMs": 10000
  }
}]
```

### Show Widget (ID: 100000)

The `id` field must be exactly `100000` to show a widget.

### Hide Widget (ID: 100001)

```json
[{
  "id": 100001,
  "createdAt": 1760036253509
}]
```

### Required Fields

- `id` (number): **100000** to show widget, **100001** to hide
- `createdAt` (number): Unix timestamp in milliseconds (used for deduplication)
- `extra` (object): Widget configuration (required for ID 100000)
- `extra.url` (string): **Required.** URL of the donatex.gg widget

## ⚙️ Configuration Parameters

All parameters are inside the `extra` object:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `url` | string | - | **Required.** URL of the donatex.gg widget |
| `setupX` | number | 0 | X position for setupResolutionX |
| `setupY` | number | 0 | Y position for setupResolutionY |
| `setupResolutionX` | number | 1920 | Reference screen width (your design resolution) |
| `setupResolutionY` | number | 1080 | Reference screen height (your design resolution) |
| `scale` | number | 1.0 | Content zoom level (0.1 - 10.0) |
| `contentZoom` | number | 1.0 | Alternative to scale |
| `opacity` | number | 1.0 | Widget opacity (0.0 - 1.0) |
| `volume` | number | 0.5 | Audio/video volume (0.0 - 1.0) |
| `clickThrough` | boolean | false | Allow clicks to pass through widget |
| `durationMs` | number | 0 | Auto-close after N milliseconds (0 = manual close) |

### Example donatex.gg Widget URLs

```
https://donatex.gg/widgets/custom-ai-widget/{YOUR-WIDGET-ID}
https://test.donatex.gg/widgets/custom-ai-widget/{YOUR-WIDGET-ID}
```

### Position Calculation

The widget automatically scales positions based on current screen resolution:

```
actualX = setupX * (currentWidth / setupResolutionX)
actualY = setupY * (currentHeight / setupResolutionY)
```

This ensures consistent positioning across different screen resolutions.

## 🏗️ Architecture

```
FixWidget
├── App.xaml.cs              # Application entry point, ntfy connection
├── WidgetWindow.xaml.cs     # Widget window with WebView2
├── Models/
│   ├── AppConfig.cs         # Application configuration
│   ├── NtfyEnvelope.cs      # ntfy message wrapper
│   ├── NtfyMessage.cs       # Widget message model
│   └── WidgetExtra.cs       # Widget parameters
└── Services/
    └── NtfyService.cs       # ntfy SSE client
```

## 🛠️ Technologies

- **WPF** - Windows Presentation Foundation
- **WebView2** - Chromium-based web rendering
- **ntfy** - Simple pub-sub notification service
- **.NET 8.0** - Modern .NET framework
- **System.Text.Json** - High-performance JSON parsing

## 🔧 Building from Source

```bash
# Clone repository
git clone https://github.com/yourusername/fixwidget.git
cd fixwidget

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## 🎨 Use Cases

- **Streaming donation alerts** - Display donatex.gg donation alerts on your desktop/OBS
- **Custom AI widgets** - Show AI-generated donation messages and animations
- **Real-time notifications** - Subscriber alerts, follower notifications
- **Desktop overlays** - Keep donation widgets always visible
- **Multi-monitor setups** - Position widgets precisely across multiple screens
- **Interactive alerts** - Animated donation alerts with sound

## 🔗 donatex.gg Integration

This application is specifically designed to work with [donatex.gg](https://donatex.gg) donation alert platform:

1. Create a widget on donatex.gg
2. Copy your widget URL (e.g., `https://donatex.gg/widgets/custom-ai-widget/YOUR-ID`)
3. Configure donatex.gg to send notifications to your ntfy topic
4. FixWidget will automatically display the widget when donations are received

The application handles the specific message format that donatex.gg sends, including:
- Wrapped JSON messages from ntfy
- Array format widget data
- Steam ID and user info fields
- Custom widget parameters

## 🔒 Security Features

- Automatic cache cleanup on startup
- Isolated WebView2 sessions
- No persistent storage of sensitive data
- Local-only operation (except ntfy connection)

## ⚡ Performance

- **Startup time**: < 2 seconds
- **Memory usage**: ~50-100 MB (depends on widget content)
- **CPU usage**: Minimal when idle
- **Widget display latency**: < 500ms from notification

## 🐛 Troubleshooting

### Widget doesn't appear

1. Check ntfy connection in system tray icon
2. Verify message format (must include `extra` object)
3. Ensure `id` is exactly `100000`
4. Check Debug output (run from terminal to see logs)

### No sound in widget

- Volume is controlled by `volume` parameter (0.0 - 1.0)
- Autoplay is enabled by default
- Check browser console in widget for errors

### Position is incorrect

- Verify `setupResolutionX` and `setupResolutionY` match your design resolution
- Position is automatically scaled to current screen resolution

## 📝 License

MIT License - see [LICENSE](LICENSE) file for details

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Contact

- Create an issue for bug reports or feature requests

## 🙏 Acknowledgments

- [ntfy](https://ntfy.sh) - Simple notification service
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Chromium-based web control
- [WPF](https://github.com/dotnet/wpf) - Windows Presentation Foundation

---

Made with ❤️ for the streaming and desktop customization community
