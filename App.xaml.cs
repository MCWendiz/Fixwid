using System.IO;
using System.Text.Json;
using System.Windows;
using FixWidget.Models;
using FixWidget.Services;

namespace FixWidget;

/// <summary>
/// Main application class for FixWidget.
/// Manages system tray icon, ntfy connection, and widget lifecycle.
/// </summary>
public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private NtfyService? _ntfyService;
    private WidgetWindow? _widgetWindow;
    private AppConfig? _config;
    private System.Drawing.Icon? _goodIcon;
    private System.Drawing.Icon? _badIcon;
    private readonly SemaphoreSlim _widgetSemaphore = new SemaphoreSlim(1, 1);
    private CancellationTokenSource? _widgetCancellation;
    private long _lastMessageCreatedAt = 0;

    /// <summary>
    /// Called when the application starts. Initializes exception handlers, loads config,
    /// creates system tray icon, and connects to ntfy server.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"❌ UNHANDLED EXCEPTION: {ex?.GetType().Name} - {ex?.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack: {ex?.StackTrace}");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"❌ DISPATCHER EXCEPTION: {args.Exception.GetType().Name} - {args.Exception.Message}");
            
            // Ignore E_ABORT errors from WebView2 (happens when window is closing)
            if (args.Exception is System.Runtime.InteropServices.COMException comEx && 
                (uint)comEx.HResult == 0x80004004)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ E_ABORT ignored (WebView2 window closing)");
                args.Handled = true;
            }
        };

        // Initialize application
        CleanupOldWebView2Sessions();
        LoadConfig();
        InitializeTrayIcon();
        ConnectToNtfy();
    }

    /// <summary>
    /// Loads configuration from cfgwidget.json or creates default config file.
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cfgwidget.json");
            
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<AppConfig>(json);
            }
            else
            {
                _config = new AppConfig();
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
        }
        catch
        {
            _config = new AppConfig();
        }
    }

    /// <summary>
    /// Cleans up old WebView2 session folders from temp directory.
    /// </summary>
    private void CleanupOldWebView2Sessions()
    {
        try
        {
            var webView2Folder = Path.Combine(Path.GetTempPath(), "FixWidget", "WebView2");
            
            if (!Directory.Exists(webView2Folder))
                return;

            var sessionFolders = Directory.GetDirectories(webView2Folder, "Session_*");
            
            if (sessionFolders.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("✅ No old WebView2 sessions to clean up");
                return;
            }
            
            foreach (var folder in sessionFolders)
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                    System.Diagnostics.Debug.WriteLine($"🧹 Deleted old WebView2 folder: {Path.GetFileName(folder)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Could not delete folder {Path.GetFileName(folder)}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"✅ WebView2 cleanup completed ({sessionFolders.Length} folders processed)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cleaning WebView2 folders: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes system tray icon with context menu.
    /// </summary>
    private void InitializeTrayIcon()
    {
        LoadIcons();

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Visible = true,
            Text = "FixWidget - Connecting..."
        };

        SetBadIcon();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => 
        {
            _notifyIcon.Visible = false;
            Shutdown();
        };
        
        contextMenu.Items.Add(exitItem);
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    /// <summary>
    /// Loads good.ico and bad.ico from embedded resources.
    /// </summary>
    private void LoadIcons()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            
            // good.ico
            var goodResourceName = "FixWidget.good.ico";
            using (var stream = assembly.GetManifestResourceStream(goodResourceName))
            {
                if (stream != null)
                {
                    _goodIcon = new System.Drawing.Icon(stream);
                    System.Diagnostics.Debug.WriteLine($"✅ Loaded embedded icon: good.ico");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Resource not found: {goodResourceName}");
                    var resources = assembly.GetManifestResourceNames();
                    System.Diagnostics.Debug.WriteLine($"Available resources: {string.Join(", ", resources)}");
                }
            }

            // bad.ico
            var badResourceName = "FixWidget.bad.ico";
            using (var stream = assembly.GetManifestResourceStream(badResourceName))
            {
                if (stream != null)
                {
                    _badIcon = new System.Drawing.Icon(stream);
                    System.Diagnostics.Debug.WriteLine($"✅ Loaded embedded icon: bad.ico");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Resource not found: {badResourceName}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading embedded icons: {ex.Message}");
        }
    }

    /// <summary>
    /// Connects to ntfy server and subscribes to events.
    /// </summary>
    private async void ConnectToNtfy()
    {
        try
        {
            _ntfyService = new NtfyService();
            
            _ntfyService.MessageReceived += OnMessageReceived;
            _ntfyService.Connected += OnConnected;
            _ntfyService.Disconnected += OnDisconnected;
            _ntfyService.ErrorOccurred += OnErrorOccurred;

            if (_config != null && !string.IsNullOrEmpty(_config.NtfyUrl))
            {
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await _ntfyService.ConnectAsync(_config.NtfyUrl);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Critical error: {ex.Message}");
            SetBadIcon();
        }
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetGoodIcon();
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = "FixWidget - Connected";
            }
        });
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetBadIcon();
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = "FixWidget - Disconnected";
            }
        });
    }

    private void OnErrorOccurred(object? sender, Exception e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetBadIcon();
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = $"FixWidget - Error: {e.Message}";
            }
        });
    }

    /// <summary>
    /// Handles incoming ntfy messages. Shows widget for ID 100000, hides for ID 100001.
    /// </summary>
    private void OnMessageReceived(object? sender, NtfyMessage message)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine($"📨 OnMessageReceived: ID={message.Id}, CreatedAt={message.CreatedAt}, Extra={message.Extra != null}");
            
            if (message.Id == 100000 && message.Extra != null)
            {
                // Deduplication check - ignore duplicate CreatedAt timestamps
                if (message.CreatedAt.HasValue && message.CreatedAt.Value > 0 && message.CreatedAt.Value == _lastMessageCreatedAt)
                {
                    System.Diagnostics.Debug.WriteLine($"⏭️ Duplicate message ignored (CreatedAt={message.CreatedAt})");
                    return;
                }
                
                _lastMessageCreatedAt = message.CreatedAt ?? 0;
                System.Diagnostics.Debug.WriteLine($"✅ Calling ShowWidgetAsync with URL={message.Extra.Url}");
                await ShowWidgetAsync(message.Extra);
            }
            else if (message.Id == 100001)
            {
                System.Diagnostics.Debug.WriteLine($"🔴 Received ID=100001, hiding widget");
                await HideWidgetAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Message ignored: ID={message.Id}, Extra is null={message.Extra == null}");
            }
        });
    }

    /// <summary>
    /// Shows widget with given configuration. Cancels previous widget if already showing.
    /// </summary>
    private async Task ShowWidgetAsync(WidgetExtra config)
    {
        // Cancel previous request if still executing
        _widgetCancellation?.Cancel();
        _widgetCancellation?.Dispose();
        _widgetCancellation = new CancellationTokenSource();
        
        var token = _widgetCancellation.Token;
        
        // Ensure only one widget operation at a time
        await _widgetSemaphore.WaitAsync(token);
        
        try
        {
            System.Diagnostics.Debug.WriteLine("=== SHOW WIDGET REQUEST ===");
            System.Diagnostics.Debug.WriteLine($"Closing old widget...");
            
            await HideWidgetAsync();
            
            if (token.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Widget creation cancelled (new request arrived)");
                return;
            }
            
            // Wait for WebView2 resources to be fully released
            await Task.Delay(300, token);
            
            if (token.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Widget creation cancelled (new request arrived)");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Creating new widget window...");
            
            try
            {
                _widgetWindow = new WidgetWindow(config);
                _widgetWindow.Show();
                
                System.Diagnostics.Debug.WriteLine($"✅ Widget window created and shown");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creating widget window: {ex.Message}");
                _widgetWindow = null;
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("⏹️ Widget creation cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Unexpected error in ShowWidgetAsync: {ex.Message}");
        }
        finally
        {
            _widgetSemaphore.Release();
        }
    }

    /// <summary>
    /// Closes current widget window if exists.
    /// </summary>
    private async Task HideWidgetAsync()
    {
        if (_widgetWindow != null)
        {
            System.Diagnostics.Debug.WriteLine("Closing existing widget window...");
            var windowToClose = _widgetWindow;
            _widgetWindow = null;
            
            try
            {
                windowToClose.Close();
                await Task.Delay(100);
                System.Diagnostics.Debug.WriteLine("Widget window closed");
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Window already closing: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error closing window: {ex.Message}");
            }
        }
    }

    private void SetGoodIcon()
    {
        if (_notifyIcon == null) return;

        if (_goodIcon != null)
        {
            _notifyIcon.Icon = _goodIcon;
            System.Diagnostics.Debug.WriteLine("✅ Set icon: good.ico");
        }
        else
        {
            _notifyIcon.Icon = System.Drawing.SystemIcons.Information;
            System.Diagnostics.Debug.WriteLine("⚠️ Using system icon (good.ico not loaded)");
        }
    }

    private void SetBadIcon()
    {
        if (_notifyIcon == null) return;

        if (_badIcon != null)
        {
            _notifyIcon.Icon = _badIcon;
            System.Diagnostics.Debug.WriteLine("✅ Set icon: bad.ico");
        }
        else
        {
            _notifyIcon.Icon = System.Drawing.SystemIcons.Error;
            System.Diagnostics.Debug.WriteLine("⚠️ Using system icon (bad.ico not loaded)");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ntfyService?.Disconnect();
        _ntfyService?.Dispose();
        _widgetWindow?.Close();
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _goodIcon?.Dispose();
        _badIcon?.Dispose();
        
        base.OnExit(e);
    }
}
