# Usage Examples

## Understanding the Message Format

FixWidget works with [donatex.gg](https://donatex.gg) and expects messages in a specific format. When donatex.gg sends a notification via ntfy, it arrives wrapped in an ntfy envelope:

```json
{
  "id": "ntfy-message-id",
  "time": 1234567890,
  "event": "message",
  "topic": "your-topic",
  "message": "[{\"id\":100000,\"extra\":{...}}]"
}
```

The `message` field contains the actual widget data as a JSON string. FixWidget automatically extracts and parses this.

## donatex.gg Widget Display

### Basic Donation Alert Widget

```bash
curl -d '[{
  "id": 100000,
  "steamid64": "-1",
  "login": "Donor Name",
  "text": "Thank you message",
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
}]' https://ntfy.sh/your-topic
```

### Test Environment Widget

```bash
curl -d '[{
  "id": 100000,
  "createdAt": 1760036253509,
  "extra": {
    "url": "https://test.donatex.gg/widgets/custom-ai-widget/YOUR-WIDGET-ID",
    "setupX": 100,
    "setupY": 100,
    "setupResolutionX": 1920,
    "setupResolutionY": 1080,
    "durationMs": 5000
  }
}]' https://ntfy.sh/your-topic
```

## Hiding Widgets

```bash
curl -d '[{
  "id": 100001,
  "createdAt": 1760036253509,
  "extra": {}
}]' https://ntfy.sh/your-topic
```

**Note**: ID `100001` is reserved for hiding all widgets. The message must still be wrapped in an array and include `createdAt` and `extra` fields.

## Configuration Parameters

### Required Fields

- **id**: `100000` to show widget, `100001` to hide all widgets
- **createdAt**: Unix timestamp in milliseconds (prevents duplicate displays)
- **extra.url**: donatex.gg widget URL (required for showing widgets)

### Optional Fields for donatex.gg Integration

- **steamid64**: Steam user ID (donatex.gg specific)
- **login**: Donor username (donatex.gg specific)  
- **text**: Donation message text (donatex.gg specific)

### Display Settings (all optional)

- **setupX**, **setupY**: Position in pixels (relative to setup resolution)
- **setupResolutionX**, **setupResolutionY**: Reference resolution for scaling (default: 2560×1440)
- **scale**: Widget scale multiplier (default: 1.0)
- **opacity**: Widget opacity 0.0–1.0 (default: 1.0)
- **volume**: Audio volume 0.0–1.0 (default: 1.0)
- **clickThrough**: Allow clicks to pass through (default: true)
- **durationMs**: Auto-hide time in milliseconds (default: 5000, max: 60000)

## Position Scaling

FixWidget automatically scales widget positions based on current screen resolution relative to your setup resolution:

```
actualX = (setupX / setupResolutionX) * CurrentScreenWidth
actualY = (setupY / setupResolutionY) * CurrentScreenHeight
```

Or more simply:
```
actualX = setupX * (CurrentScreenWidth / setupResolutionX)
actualY = setupY * (CurrentScreenHeight / setupResolutionY)
```

**Example**: 
- Setup: Widget at `setupX=500, setupY=500` on `setupResolution=1000×1000`
- Client screen: `2000×2000`
- Result: Widget displays at `1000, 1000`
- Calculation: `500 * (2000/1000) = 500 * 2 = 1000`

This ensures widgets appear at the same **relative position** regardless of screen resolution.

## donatex.gg Integration

### How It Works

1. **Donation occurs** on donatex.gg
2. **donatex.gg sends notification** to your configured ntfy topic
3. **FixWidget receives** the notification and displays the custom widget
4. **Widget shows** donation alert with animations/effects
5. **Auto-hides** after configured duration

### Widget URLs

- Production: `https://donatex.gg/widgets/custom-ai-widget/{YOUR-ID}`
- Test environment: `https://test.donatex.gg/widgets/custom-ai-widget/{YOUR-ID}`

### Typical Message Flow

```
Donation → donatex.gg → ntfy topic → FixWidget → Display Widget Overlay
```

## PowerShell Script Examples

### Send ntfy Test Message

```powershell
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$message = @"
[{
  "id": 100000,
  "steamid64": "-1",
  "login": "TestDonor",
  "text": "Test donation message",
  "createdAt": $timestamp,
  "extra": {
    "url": "https://donatex.gg/widgets/custom-ai-widget/YOUR-WIDGET-ID",
    "setupX": 500,
    "setupY": 500,
    "setupResolutionX": 2560,
    "setupResolutionY": 1440,
    "durationMs": 5000
  }
}]
"@

Invoke-RestMethod -Uri "https://ntfy.sh/your-topic" -Method Post -Body $message -ContentType "application/json"
```

### Hide All Widgets

```powershell
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$message = @"
[{
  "id": 100001,
  "createdAt": $timestamp,
  "extra": {}
}]
"@

Invoke-RestMethod -Uri "https://ntfy.sh/your-topic" -Method Post -Body $message -ContentType "application/json"
```

## Python Integration Example

```python
import requests
import time

def show_donatex_widget(widget_id, donor_name="Anonymous", message_text="", x=500, y=500):
    """Send donatex.gg widget display message via ntfy"""
    payload = [{
        "id": 100000,
        "steamid64": "-1",
        "login": donor_name,
        "text": message_text,
        "createdAt": int(time.time() * 1000),
        "extra": {
            "url": f"https://donatex.gg/widgets/custom-ai-widget/{widget_id}",
            "setupX": x,
            "setupY": y,
            "setupResolutionX": 2560,
            "setupResolutionY": 1440,
            "durationMs": 10000
        }
    }]
    
    requests.post(
        'https://ntfy.sh/your-topic',
        json=payload
    )

# Usage
show_donatex_widget('YOUR-WIDGET-ID', donor_name='JohnDoe', message_text='Thanks for streaming!')
```

## Node.js Integration Example

```javascript
const fetch = require('node-fetch');

async function showDonatexWidget(widgetId, options = {}) {
  const message = [{
    id: 100000,
    steamid64: options.steamId || "-1",
    login: options.donorName || "Anonymous",
    text: options.messageText || "",
    createdAt: Date.now(),
    extra: {
      url: `https://donatex.gg/widgets/custom-ai-widget/${widgetId}`,
      setupX: options.x || 500,
      setupY: options.y || 500,
      setupResolutionX: 2560,
      setupResolutionY: 1440,
      durationMs: options.duration || 10000
    }
  }];
  
  await fetch('https://ntfy.sh/your-topic', {
    method: 'POST',
    body: JSON.stringify(message),
    headers: { 'Content-Type': 'application/json' }
  });
}

// Usage
showDonatexWidget('YOUR-WIDGET-ID', {
  donorName: 'JohnDoe',
  messageText: 'Love the stream!',
  x: 100,
  y: 100,
  duration: 5000
});
```

## Tips

1. **Always wrap messages in array format**: `[{...}]` not `{...}`
2. **Use unique `createdAt` timestamps** to avoid message deduplication
3. **Set `durationMs: 0`** for persistent widgets (close manually with ID 100001)
4. **Test with donatex.gg test environment** before using production widgets
5. **Use `clickThrough: true`** for overlays that shouldn't block mouse interactions
6. **Configure position at your design resolution** - scaling handles other resolutions automatically
