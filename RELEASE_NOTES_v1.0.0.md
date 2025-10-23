# FixWidget v1.0.0 - Initial Release

## 🎉 First Stable Release

FixWidget is a Windows desktop application that displays **donatex.gg donation widgets** as transparent overlays on your screen in real-time via ntfy notifications.

---

## ✨ Features

### Core Functionality
- **donatex.gg Integration**: Display custom donation alert widgets from donatex.gg platform
- **Real-time Notifications**: Connects to ntfy server for instant widget display
- **Transparent Overlays**: Fully customizable transparency and click-through support
- **Auto-positioning**: Smart position scaling across different screen resolutions
- **Audio/Video Support**: Automatic playback with volume control (no user interaction required)
- **Multi-monitor Ready**: Position widgets on any monitor with automatic scaling

### Technical Highlights
- **WebView2 Integration**: Modern Chromium-based rendering engine
- **Automatic Cache Cleanup**: Cleans old WebView2 sessions on startup
- **Message Deduplication**: Prevents duplicate widget displays
- **Background Service**: Runs in system tray with connection status indicators
- **Error Handling**: Robust error recovery with automatic reconnection

---

## 📋 System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 8.0 Runtime
- **Browser**: Microsoft Edge WebView2 Runtime (auto-installed)
- **Internet**: Required for ntfy connection and widget content

---

## 🚀 Quick Start

### Installation

1. **Download** `FixWidget.exe` from this release
2. **Install .NET 8.0 Runtime** if not already installed:
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
3. **Run** `FixWidget.exe` - WebView2 will auto-install if needed

### Configuration

Create `cfgwidget.json` in the same folder as `FixWidget.exe`:

```json
{
  "NtfyUrl": "https://ntfy.sh/your-topic-name"
}
```

Or let FixWidget create a default config on first run.

### Usage

1. **Start FixWidget** - green tray icon = connected
2. **Configure donatex.gg** to send notifications to your ntfy topic
3. **Donations appear automatically** as overlay widgets

---

## 📝 Message Format

FixWidget expects messages in this format from ntfy:

```json
[{
  "id": 100000,
  "steamid64": "-1",
  "login": "DonorName",
  "text": "Thank you!",
  "createdAt": 1760036253509,
  "extra": {
    "url": "https://donatex.gg/widgets/custom-ai-widget/YOUR-ID",
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

**Command IDs:**
- `100000` - Show widget
- `100001` - Hide all widgets

---

## 🔧 Configuration Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `url` | string | *required* | Widget URL (donatex.gg widget URL) |
| `setupX` | int | 0 | X position at setup resolution |
| `setupY` | int | 0 | Y position at setup resolution |
| `setupResolutionX` | int | 2560 | Reference screen width for scaling |
| `setupResolutionY` | int | 1440 | Reference screen height for scaling |
| `scale` | double | 1.0 | Widget scale multiplier |
| `opacity` | double | 1.0 | Widget opacity (0.0 - 1.0) |
| `volume` | double | 1.0 | Audio/video volume (0.0 - 1.0) |
| `clickThrough` | bool | true | Allow clicks to pass through widget |
| `durationMs` | int | 5000 | Auto-hide delay in milliseconds (max 60000) |

---

## 🎯 Use Cases

- **Streamers**: Show donation alerts during live streams
- **Content Creators**: Display custom widgets with animations
- **Event Displays**: Real-time notifications for events
- **Custom Overlays**: Any web-based content as transparent overlay

---

## 🐛 Known Issues

None reported yet! Please open an issue if you find any bugs.

---

## 📚 Documentation

- [README.md](README.md) - Complete documentation
- [USAGE.md](USAGE.md) - Usage examples with PowerShell, Python, Node.js
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture overview
- [CONTRIBUTING.md](CONTRIBUTING.md) - Contribution guidelines

---

## 🙏 Credits

Powered by:
- [ntfy](https://ntfy.sh) - Simple pub-sub notification service
- [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) - Microsoft Edge WebView2 Runtime
- [donatex.gg](https://donatex.gg) - Donation platform integration

---

## 📄 License

This project is licensed under the MIT License - see [LICENSE](LICENSE) file for details.

---

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/MCWendiz/fixwid/issues)
- **Discussions**: [GitHub Discussions](https://github.com/MCWendiz/fixwid/discussions)

---

## 🔮 Future Plans

- [ ] GUI configuration editor
- [ ] Multiple widget queue support
- [ ] Widget templates library
- [ ] Performance optimizations
- [ ] Linux/macOS support

---

**Thank you for using FixWidget!** ⭐

If you find this project useful, please consider starring the repository.
