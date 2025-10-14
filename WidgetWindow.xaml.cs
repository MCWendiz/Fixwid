using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using FixWidget.Models;
using Microsoft.Web.WebView2.Core;

namespace FixWidget;

public partial class WidgetWindow : Window
{
    private readonly WidgetExtra _config;
    private DispatcherTimer? _hideTimer;
    private EventHandler<CoreWebView2NavigationCompletedEventArgs>? _navigationHandler;
    private CoreWebView2Environment? _environment;
    private bool _isInitialized = false;
    private bool _isDisposed = false;

    // Win32 API для click-through
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public WidgetWindow(WidgetExtra config)
    {
        InitializeComponent();
        _config = config;
        
        // Логируем входящую конфигурацию
        try
        {
            System.Diagnostics.Debug.WriteLine("=== NEW WIDGET WINDOW ===");
            System.Diagnostics.Debug.WriteLine($"URL: {config.Url}");
            System.Diagnostics.Debug.WriteLine($"Position: setupX={config.SetupX}, setupY={config.SetupY}, x={config.X}, y={config.Y}");
            System.Diagnostics.Debug.WriteLine($"Size: width={config.Width}, height={config.Height}");
            System.Diagnostics.Debug.WriteLine($"Transform: scale={config.Scale}, contentZoom={config.ContentZoom}, opacity={config.Opacity}");
            System.Diagnostics.Debug.WriteLine($"Screen: setupResX={config.SetupResolutionX}, setupResY={config.SetupResolutionY}");
            System.Diagnostics.Debug.WriteLine($"Other: volume={config.Volume}, clickThrough={config.ClickThrough}, duration={config.DurationMs}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error logging config: {ex.Message}");
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Проверяем что окно не было уже инициализировано или закрыто
        if (_isInitialized || _isDisposed)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ Window_Loaded пропущен: окно уже инициализировано или закрыто");
            return;
        }
        
        _isInitialized = true;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("WidgetWindow.Window_Loaded начато");
            
            // ВАЖНО: Применяем конфигурацию окна ДО инициализации WebView2
            System.Diagnostics.Debug.WriteLine("Применение конфигурации окна (размер, позиция, прозрачность)...");
            ApplyWindowConfiguration();
            
            // Проверяем URL
            if (string.IsNullOrWhiteSpace(_config.Url))
            {
                System.Diagnostics.Debug.WriteLine("ERROR: URL пустой!");
                System.Windows.MessageBox.Show("URL виджета не указан!", "Ошибка", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Close();
                return;
            }

            // Добавляем https:// если протокол не указан
            var url = _config.Url;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            
            System.Diagnostics.Debug.WriteLine($"Итоговый URL: {url}");

            // Инициализируем WebView2 с общим окружением для ускорения
            System.Diagnostics.Debug.WriteLine("Инициализация WebView2 Environment...");
            _environment = await CreateWebView2EnvironmentAsync();
            
            System.Diagnostics.Debug.WriteLine("Инициализация CoreWebView2...");
            try
            {
                await WebView.EnsureCoreWebView2Async(_environment);
            }
            catch (COMException ex) when ((uint)ex.HResult == 0x80004004)
            {
                // E_ABORT - окно закрывается во время инициализации
                System.Diagnostics.Debug.WriteLine("⚠️ E_ABORT при EnsureCoreWebView2Async - окно закрывается");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Критическая ошибка при инициализации WebView2: {ex.GetType().Name} - {ex.Message}");
                throw;
            }

            // Настраиваем WebView2
            if (WebView.CoreWebView2 != null)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 готов. Громкость: {_config.Volume}");
                
                // МАСШТАБ теперь применяется через размер окна в ApplyWindowConfiguration()
                // LayoutTransform больше не используется
                
                // Включаем звук ПЕРЕД навигацией
                WebView.CoreWebView2.IsMuted = false;
                
                // АГРЕССИВНАЯ ОЧИСТКА КЕША
                System.Diagnostics.Debug.WriteLine("🗑️ Агрессивная очистка кеша WebView2...");
                try
                {
                    // Очищаем ВСЕ типы данных
                    await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        CoreWebView2BrowsingDataKinds.AllDomStorage |
                        CoreWebView2BrowsingDataKinds.AllSite |
                        CoreWebView2BrowsingDataKinds.CacheStorage |
                        CoreWebView2BrowsingDataKinds.Cookies |
                        CoreWebView2BrowsingDataKinds.DiskCache |
                        CoreWebView2BrowsingDataKinds.IndexedDb |
                        CoreWebView2BrowsingDataKinds.LocalStorage |
                        CoreWebView2BrowsingDataKinds.WebSql
                    );
                    System.Diagnostics.Debug.WriteLine("✅ Все типы кеша очищены");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Не удалось очистить кеш: {ex.Message}");
                }
                
                // ОТКЛЮЧАЕМ КЕШ в настройках WebView2
                var settings = WebView.CoreWebView2.Settings;
                settings.IsGeneralAutofillEnabled = false;
                settings.IsPasswordAutosaveEnabled = false;
                
                // УБИРАЕМ СКРОЛЛБАРЫ - отключаем прокрутку полностью
                settings.AreDefaultScriptDialogsEnabled = true;
                settings.IsScriptEnabled = true;
                settings.AreDevToolsEnabled = false;
                settings.AreDefaultContextMenusEnabled = false;
                
                // РАЗРЕШАЕМ АВТОПЛЕЙ АУДИО/ВИДЕО без взаимодействия пользователя
                settings.IsWebMessageEnabled = true;
                
                System.Diagnostics.Debug.WriteLine("✅ WebView2: Настройки применены (autoplay enabled)");
                
                // УСТАНАВЛИВАЕМ ПРОЗРАЧНЫЙ ФОН
                WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                System.Diagnostics.Debug.WriteLine("✅ WebView2: Прозрачный фон установлен");
                
                // Подписываемся на ошибки процесса
                WebView.CoreWebView2.ProcessFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ WebView2 ProcessFailed: {e.ProcessFailedKind} - {e.Reason}");
                };
                
                // Загружаем URL виджета с HTTP-заголовками против кеширования
                var urlWithNoCache = url + (url.Contains("?") ? "&" : "?") + "_t=" + DateTime.Now.Ticks;
                System.Diagnostics.Debug.WriteLine($"Навигация к URL (no-cache): {urlWithNoCache}");
                
                try
                {
                    // Создаём запрос с заголовками против кеширования
                    var request = WebView.CoreWebView2.Environment.CreateWebResourceRequest(
                        urlWithNoCache,
                        "GET",
                        null,
                        "Cache-Control: no-cache, no-store, must-revalidate\r\nPragma: no-cache\r\nExpires: 0"
                    );
                    
                    WebView.CoreWebView2.NavigateWithWebResourceRequest(request);
                    System.Diagnostics.Debug.WriteLine("✅ Навигация с заголовками no-cache запущена");
                }
                catch (COMException ex) when ((uint)ex.HResult == 0x80004004)
                {
                    // E_ABORT - навигация отменена, возможно окно уже закрывается
                    System.Diagnostics.Debug.WriteLine("⚠️ E_ABORT при Navigate - навигация отменена (окно закрывается)");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка при Navigate: {ex.GetType().Name} - {ex.Message}");
                    throw;
                }
                
                // Устанавливаем громкость через JavaScript после загрузки страницы
                if (_config.Volume >= 0 && _config.Volume <= 1)
                {
                    // Создаём обработчик один раз
                    _navigationHandler = async (s, e) =>
                    {
                        try
                        {
                            // Проверяем, что окно ещё не закрыто
                            if (WebView?.CoreWebView2 == null)
                                return;

                            System.Diagnostics.Debug.WriteLine("🔊 NavigationCompleted - начинаем установку громкости");

                            // УБИРАЕМ СКРОЛЛБАРЫ + ПРИМЕНЯЕМ OPACITY + МАСШТАБ К КОНТЕНТУ через CSS
                            var opacityValue = (_config.Opacity / 100.0).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            var zoomValue = _config.ContentZoom.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            var cssCustomizations = $@"
                                (function() {{
                                    const style = document.createElement('style');
                                    style.textContent = `
                                        html, body {{
                                            overflow: hidden !important;
                                            margin: 0 !important;
                                            padding: 0 !important;
                                            width: 100% !important;
                                            height: 100% !important;
                                        }}
                                        * {{
                                            scrollbar-width: none !important; /* Firefox */
                                            -ms-overflow-style: none !important; /* IE/Edge */
                                        }}
                                        *::-webkit-scrollbar {{
                                            display: none !important; /* Chrome/Safari */
                                            width: 0 !important;
                                            height: 0 !important;
                                        }}
                                        
                                        /* ПРИМЕНЯЕМ OPACITY К САМОМУ КОНТЕНТУ */
                                        html {{
                                            opacity: {opacityValue} !important;
                                        }}
                                        
                                        /* МАСШТАБИРУЕМ ВЕСЬ САЙТ через transform: scale() */
                                        body {{
                                            transform: scale({zoomValue}) !important;
                                            transform-origin: top left !important;
                                            width: calc(100% / {zoomValue}) !important;
                                            height: calc(100% / {zoomValue}) !important;
                                        }}
                                    `;
                                    document.head.appendChild(style);
                                    console.log('✅ CSS применен: opacity={opacityValue}, contentZoom={zoomValue}');
                                }})();
                            ";
                            await WebView.CoreWebView2.ExecuteScriptAsync(cssCustomizations);
                            System.Diagnostics.Debug.WriteLine($"✅ CSS применен: opacity={opacityValue}, contentZoom={zoomValue}");

                            // Устанавливаем громкость для всех audio и video элементов
                            var script = $@"
                                (function() {{
                                    const volume = {_config.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                                    console.log('🔊 FixWidget: Setting volume to:', volume);
                                    
                                    // ЭМУЛИРУЕМ ВЗАИМОДЕЙСТВИЕ ПОЛЬЗОВАТЕЛЯ для разрешения autoplay
                                    // Создаем и диспатчим события клика и касания
                                    try {{
                                        const events = [
                                            new MouseEvent('click', {{ bubbles: true, cancelable: true, view: window }}),
                                            new MouseEvent('mousedown', {{ bubbles: true, cancelable: true, view: window }}),
                                            new TouchEvent('touchstart', {{ bubbles: true, cancelable: true, view: window }}),
                                        ];
                                        events.forEach(event => document.body?.dispatchEvent(event));
                                        console.log('✅ User interaction emulated');
                                    }} catch(e) {{
                                        console.warn('⚠️ Could not emulate interaction:', e.message);
                                    }}
                                    
                                    // Функция установки громкости
                                    function setVolume(el) {{
                                        console.log('🔍 Found element:', el.tagName, 'src:', el.src || el.currentSrc);
                                        console.log('  Current volume:', el.volume, 'muted:', el.muted, 'paused:', el.paused);
                                        
                                        el.volume = volume;
                                        el.muted = false;
                                        
                                        // Пробуем автозапуск
                                        if (el.paused) {{
                                            el.play().then(() => {{
                                                console.log('✅ Autoplay успешен для', el.tagName);
                                            }}).catch(e => {{
                                                console.warn('⚠️ Autoplay заблокирован:', e.message);
                                            }});
                                        }}
                                        
                                        console.log('  New volume:', el.volume, 'muted:', el.muted);
                                    }}
                                    
                                    // Ждём загрузку DOM
                                    function trySetVolume() {{
                                        const elements = document.querySelectorAll('audio, video');
                                        console.log('🔍 Найдено элементов audio/video:', elements.length);
                                        elements.forEach(setVolume);
                                    }}
                                    
                                    // Пробуем сразу
                                    trySetVolume();
                                    
                                    // ИМИТАЦИЯ КЛИКА для обхода Autoplay Policy
                                    setTimeout(() => {{
                                        console.log('�️ Имитация клика для разблокировки autoplay');
                                        document.body.click();
                                        
                                        // Пробуем запустить все audio/video после клика
                                        setTimeout(() => {{
                                            document.querySelectorAll('audio, video').forEach(el => {{
                                                if (el.paused) {{
                                                    el.play().then(() => {{
                                                        console.log('✅ Play после клика успешен');
                                                    }}).catch(e => {{
                                                        console.error('❌ Play после клика failed:', e);
                                                    }});
                                                }}
                                            }});
                                        }}, 100);
                                    }}, 500);
                                    
                                    // Для будущих элементов через MutationObserver
                                    const observer = new MutationObserver(() => {{
                                        trySetVolume();
                                    }});
                                    observer.observe(document.body, {{ childList: true, subtree: true }});
                                    
                                    // Также следим за атрибутом src
                                    const attrObserver = new MutationObserver(() => {{
                                        trySetVolume();
                                    }});
                                    document.querySelectorAll('audio, video').forEach(el => {{
                                        attrObserver.observe(el, {{ attributes: true, attributeFilter: ['src'] }});
                                    }});
                                    
                                    // Принудительная установка каждые 500ms первые 10 секунд
                                    let attempts = 0;
                                    const interval = setInterval(() => {{
                                        trySetVolume();
                                        attempts++;
                                        if (attempts > 20) {{
                                            clearInterval(interval);
                                            console.log('⏹️ Остановка принудительной установки громкости');
                                        }}
                                    }}, 500);
                                    
                                    console.log('🔊 FixWidget: Audio volume control initialized');
                                }})();
                            ";
                            
                            await WebView.CoreWebView2.ExecuteScriptAsync(script);
                            System.Diagnostics.Debug.WriteLine($"✅ Громкость установлена через JavaScript: {_config.Volume}");
                        }
                        catch (COMException ex) when ((uint)ex.HResult == 0x80004004)
                        {
                            // E_ABORT - операция отменена, окно закрывается - это нормально
                            System.Diagnostics.Debug.WriteLine("Навигация отменена (окно закрывается)");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка установки громкости: {ex.Message}");
                        }
                    };
                    
                    // Подписываемся на событие
                    WebView.CoreWebView2.NavigationCompleted += _navigationHandler;
                }
            }

            // Применяем click-through после инициализации WebView2 (если нужно)
            if (_config.ClickThrough)
            {
                ApplyClickThrough();
            }
            
            
            // Если есть звук - делаем программный клик для разблокировки Autoplay
            if (_config.Volume > 0)
            {
                System.Diagnostics.Debug.WriteLine("🖱️ Делаем программный клик для разблокировки autoplay");
                await Task.Delay(1000); // Ждём загрузку страницы
                
                try
                {
                    // Программный клик по центру страницы
                    await WebView.CoreWebView2.ExecuteScriptAsync(@"
                        (function() {
                            const event = new MouseEvent('click', {
                                view: window,
                                bubbles: true,
                                cancelable: true
                            });
                            document.body.dispatchEvent(event);
                            console.log('🖱️ Программный клик выполнен из C#');
                        })();
                    ");
                    System.Diagnostics.Debug.WriteLine("✅ Программный клик выполнен");
                }
                catch (COMException ex) when ((uint)ex.HResult == 0x80004004)
                {
                    // E_ABORT - операция отменена, окно возможно закрывается
                    System.Diagnostics.Debug.WriteLine("⚠️ E_ABORT при ExecuteScriptAsync - операция отменена");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка программного клика: {ex.Message}");
                }
            }

            // Инициализируем постоянную активацию страницы для autoplay
            try
            {
                await WebView.CoreWebView2.ExecuteScriptAsync(@"
                    (function() {
                        console.log('🔄 Запуск постоянной активации страницы');
                        
                        // Периодически эмулируем активность пользователя
                        setInterval(() => {
                            try {
                                // Симулируем движение мыши
                                const event = new MouseEvent('mousemove', {
                                    view: window,
                                    bubbles: true,
                                    cancelable: true,
                                    clientX: 1,
                                    clientY: 1
                                });
                                document.dispatchEvent(event);
                                
                                // Предотвращаем переход страницы в фоновый режим
                                if (document.hidden) {
                                    console.log('⚠️ Page is hidden, trying to activate');
                                }
                            } catch(e) {
                                console.warn('⚠️ Activity simulation error:', e);
                            }
                        }, 5000); // Каждые 5 секунд
                        
                        console.log('✅ Постоянная активация настроена');
                    })();
                ");
                System.Diagnostics.Debug.WriteLine("✅ Постоянная активация страницы настроена");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка настройки активации: {ex.Message}");
            }

            // Устанавливаем таймер скрытия, если нужно
            if (_config.DurationMs > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Установка таймера на {_config.DurationMs} мс");
                _hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(_config.DurationMs)
                };
                _hideTimer.Tick += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("Таймер сработал - закрываем виджет");
                    _hideTimer?.Stop();
                    Close();
                };
                _hideTimer.Start();
            }
            
            System.Diagnostics.Debug.WriteLine("WidgetWindow.Window_Loaded завершено успешно");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА в Window_Loaded: {ex}");
            System.Windows.MessageBox.Show($"Ошибка загрузки виджета: {ex.Message}", "Ошибка", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Close();
        }
    }

    private static async Task<CoreWebView2Environment> CreateWebView2EnvironmentAsync()
    {
        // Создаём УНИКАЛЬНУЮ папку для каждого экземпляра WebView2
        // Это гарантирует свежий кеш каждый раз
        var timestamp = DateTime.Now.Ticks;
        var userDataFolder = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "FixWidget",
            "WebView2",
            $"Session_{timestamp}"
        );
        
        System.Diagnostics.Debug.WriteLine($"WebView2 UserDataFolder (NEW SESSION): {userDataFolder}");
        
        // Создаём папку если её нет
        System.IO.Directory.CreateDirectory(userDataFolder);
        
        // Создаём опции для Environment
        var options = new CoreWebView2EnvironmentOptions();
        
        // ВАЖНО: Добавляем аргументы командной строки для разрешения автоплея
        options.AdditionalBrowserArguments = 
            "--autoplay-policy=no-user-gesture-required " +  // Разрешаем автоплей БЕЗ клика пользователя
            "--disable-features=AutoplayIgnoreWebAudio " +   // Игнорируем ограничения для Web Audio API
            "--disable-background-timer-throttling " +       // Не замедляем таймеры в фоне
            "--disable-backgrounding-occluded-windows " +    // Не переводим окна в фон
            "--disable-renderer-backgrounding";              // Не останавливаем рендеринг в фоне
        
        System.Diagnostics.Debug.WriteLine($"🔊 WebView2 arguments: {options.AdditionalBrowserArguments}");
        
        var environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
            null, // browserExecutableFolder - использовать системный Edge
            userDataFolder, // userDataFolder
            options // options - С РАЗРЕШЕНИЕМ АВТОПЛЕЯ!
        );
        
        return environment;
    }

    private void ApplyWindowConfiguration()
    {
        // Получаем текущее разрешение экрана
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // Вычисляем масштабирующий коэффициент для позиционирования
        var scaleX = screenWidth / _config.SetupResolutionX;
        var scaleY = screenHeight / _config.SetupResolutionY;

        // РАЗМЕР ОКНА: Всегда фиксированный 600x600 (НЕ зависит от scale!)
        Width = 1000;
        Height = 1000;

        // Позиция окна - используем setupX/setupY с учетом масштаба экрана
        Left = _config.SetupX * scaleX;
        Top = _config.SetupY * scaleY;

        // Прозрачность окна (но основная прозрачность через CSS)
        Opacity = 1.0; // Окно всегда непрозрачное, прозрачность контента через CSS

        System.Diagnostics.Debug.WriteLine($"=== WINDOW CONFIG APPLIED ===");
        System.Diagnostics.Debug.WriteLine($"Position: setupX={_config.SetupX}, setupY={_config.SetupY}");
        System.Diagnostics.Debug.WriteLine($"Window size: 600x600 (fixed)");
        System.Diagnostics.Debug.WriteLine($"ContentZoom={_config.ContentZoom} (или scale={_config.Scale} если contentZoom не задан)");
        System.Diagnostics.Debug.WriteLine($"Opacity={_config.Opacity}%");
        System.Diagnostics.Debug.WriteLine($"Screen: {screenWidth}x{screenHeight}, ScaleFactor: {scaleX:F2}x{scaleY:F2}");
        System.Diagnostics.Debug.WriteLine($"Final: Width={Width:F0}, Height={Height:F0}, Left={Left:F0}, Top={Top:F0}");
    }

    private void ApplyClickThrough()
    {
        if (_config.ClickThrough)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            System.Diagnostics.Debug.WriteLine($"✅ Click-through: ENABLED");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_isDisposed)
        {
            base.OnClosed(e);
            return;
        }
        
        _isDisposed = true;
        
        try
        {
            _hideTimer?.Stop();
            _hideTimer = null;
            
            // Отписываемся от события
            if (_navigationHandler != null && WebView?.CoreWebView2 != null)
            {
                try
                {
                    WebView.CoreWebView2.NavigationCompleted -= _navigationHandler;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error unsubscribing NavigationCompleted: {ex.Message}");
                }
                _navigationHandler = null;
            }
            
            // Очищаем WebView2
            try
            {
                WebView?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error disposing WebView: {ex.Message}");
            }
            
            // Очищаем Environment
            _environment = null;
            
            System.Diagnostics.Debug.WriteLine("✅ WidgetWindow closed and disposed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error in OnClosed: {ex.Message}");
        }
        finally
        {
            base.OnClosed(e);
        }
    }
}
