# Architecture

## Overview

FixWidget is a WPF desktop application that displays web-based widgets as transparent overlays using WebView2, controlled via ntfy pub-sub notifications.

```
┌─────────────────────────────────────────────────────────┐
│                      User/Service                       │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP POST
                     ▼
              ┌─────────────┐
              │ ntfy Server │
              └─────────────┘
                     │ SSE Stream
                     ▼
┌─────────────────────────────────────────────────────────┐
│                      FixWidget                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │              App.xaml.cs                         │  │
│  │  - Application Entry Point                       │  │
│  │  - System Tray Management                        │  │
│  │  - Message Routing                               │  │
│  └────────────┬─────────────────────────────────────┘  │
│               │                                         │
│  ┌────────────▼─────────────────────────────────────┐  │
│  │         NtfyService.cs                           │  │
│  │  - SSE Connection                                │  │
│  │  - JSON Parsing                                  │  │
│  │  - Event Publishing                              │  │
│  └────────────┬─────────────────────────────────────┘  │
│               │ MessageReceived Event                   │
│               ▼                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │           WidgetWindow.xaml.cs                   │  │
│  │  - WebView2 Hosting                              │  │
│  │  - Widget Configuration                          │  │
│  │  - Overlay Rendering                             │  │
│  │  - Position/Scale Management                     │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Component Details

### App.xaml.cs
**Responsibility**: Application lifecycle and coordination

**Key Functions**:
- Initialize system tray icon
- Connect to ntfy service
- Route messages to widget windows
- Manage widget lifecycle (create/destroy)
- Handle deduplication

**State**:
- Current widget window reference
- Last message timestamp
- ntfy service instance
- Semaphore for synchronization

### NtfyService.cs
**Responsibility**: ntfy communication

**Key Functions**:
- Establish SSE connection to ntfy
- Parse JSON messages
- Handle reconnection
- Emit `MessageReceived` events

**Message Flow**:
1. Receive SSE line
2. Parse ntfy envelope
3. Extract inner message JSON
4. Deserialize to `NtfyMessage`
5. Validate ID (100000/100001)
6. Emit event

### WidgetWindow.xaml.cs
**Responsibility**: Widget rendering and display

**Key Functions**:
- Initialize WebView2 environment
- Load widget URL
- Apply transformations (position, scale, opacity)
- Manage audio/video settings
- Handle click-through
- Auto-close timer

**Initialization Sequence**:
1. Create window
2. Apply base configuration
3. Create WebView2 environment
4. Initialize CoreWebView2
5. Clear cache
6. Load URL with no-cache headers
7. Inject CSS customizations
8. Set volume
9. Apply click-through (optional)
10. Start auto-close timer (optional)

## Data Models

### AppConfig
```csharp
class AppConfig {
    string NtfyUrl
}
```

### NtfyEnvelope
```csharp
class NtfyEnvelope {
    string Event      // "message" | "keepalive"
    string Message    // Inner JSON string
}
```

### NtfyMessage
```csharp
class NtfyMessage {
    int Id            // 100000 = show, 100001 = hide
    long CreatedAt    // Unix timestamp
    WidgetExtra Extra // Widget configuration
}
```

### WidgetExtra
```csharp
class WidgetExtra {
    string Url
    int SetupX, SetupY
    int SetupResolutionX, SetupResolutionY
    double Scale, ContentZoom, Opacity, Volume
    bool ClickThrough
    int DurationMs
}
```

## Threading Model

- **UI Thread**: WPF main thread, handles all UI operations
- **Background Thread**: ntfy SSE reading (StreamReader)
- **Synchronization**: 
  - `Dispatcher.InvokeAsync()` for UI operations
  - `SemaphoreSlim` for widget creation serialization
  - `CancellationToken` for operation cancellation

## WebView2 Integration

### Environment Creation
- Unique user data folder per instance
- Timestamp-based folder naming
- Located in `%TEMP%\FixWidget\WebView2\Session_{timestamp}`

### Cache Management
- Old sessions cleaned up on startup (>1 day old)
- Cache cleared on each widget load
- No persistent storage

### JavaScript Injection
1. **CSS Customizations** - Hide scrollbars, apply opacity/zoom
2. **Volume Control** - Set audio/video volume with MutationObserver
3. **Autoplay Enablement** - Emulate user interaction
4. **Activity Simulation** - Periodic events to prevent backgrounding

## Security Considerations

1. **URL Validation**: URLs sanitized, https:// added if missing
2. **Cache Isolation**: Each widget gets isolated cache
3. **No Persistence**: No data stored between runs
4. **Local-only**: No external connections except ntfy
5. **DevTools Disabled**: Production builds disable debugging

## Performance Optimizations

1. **Shared HttpClient**: Single client for all ntfy connections
2. **Async/Await**: Non-blocking I/O throughout
3. **Resource Cleanup**: Explicit disposal of WebView2 resources
4. **Semaphore**: Prevents simultaneous widget creation
5. **Cancellation**: Fast cancellation of pending operations

## Error Handling

- Try-catch blocks around critical operations
- Graceful degradation (widget closes on error)
- Extensive logging for debugging
- User-friendly error messages
- Automatic reconnection for ntfy

## Extensibility Points

Future enhancements could add:
- Custom widget shapes (non-rectangular)
- Multiple simultaneous widgets
- Animation support
- Custom event handlers
- Plugin system
- Widget templates
- Configuration UI
