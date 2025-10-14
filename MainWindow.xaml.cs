using System.Windows;
using System.Windows.Media;
using FixWidget.Models;
using FixWidget.Services;

namespace FixWidget;

public partial class MainWindow : Window
{
    private readonly NtfyService _ntfyService;
    private WidgetWindow? _widgetWindow;

    private enum ConnectionState
    {
        Disconnected,
        Connected,
        Error
    }

    public MainWindow()
    {
        InitializeComponent();
        _ntfyService = new NtfyService();
        
        _ntfyService.MessageReceived += OnMessageReceived;
        _ntfyService.ErrorOccurred += OnErrorOccurred;
        _ntfyService.Connected += OnConnected;
        _ntfyService.Disconnected += OnDisconnected;

        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Закрываем все подключения и окна
        _ntfyService.Disconnect();
        _ntfyService.Dispose();
        _widgetWindow?.Close();
        _widgetWindow = null;
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ntfyService.IsConnected)
        {
            // Отключаемся
            _ntfyService.Disconnect();
            return;
        }

        var url = NtfyUrlTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(url))
        {
            SetConnectionState(ConnectionState.Error, "Введите URL для подключения");
            return;
        }

        // Подключаемся
        SetConnectionState(ConnectionState.Disconnected, null);
        ConnectButton.IsEnabled = false;

        try
        {
            // Запускаем подключение в фоновом режиме
            _ = Task.Run(async () =>
            {
                try
                {
                    await _ntfyService.ConnectAsync(url);
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        SetConnectionState(ConnectionState.Error, $"Ошибка подключения: {ex.Message}");
                        ConnectButton.IsEnabled = true;
                    });
                }
            });
        }
        catch (Exception ex)
        {
            SetConnectionState(ConnectionState.Error, $"Ошибка: {ex.Message}");
            ConnectButton.IsEnabled = true;
        }
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetConnectionState(ConnectionState.Connected, null);
            ConnectButton.IsEnabled = true;
        });
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetConnectionState(ConnectionState.Disconnected, null);
            ConnectButton.IsEnabled = true;
        });
    }

    private void OnErrorOccurred(object? sender, Exception e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            SetConnectionState(ConnectionState.Error, $"Ошибка: {e.Message}");
        });
    }

    private void OnMessageReceived(object? sender, NtfyMessage message)
    {
        Dispatcher.InvokeAsync(() =>
        {
            System.Diagnostics.Debug.WriteLine($"Получено сообщение: ID={message.Id}, Extra={message.Extra != null}");
            
            if (message.Id == 100000 && message.Extra != null)
            {
                System.Diagnostics.Debug.WriteLine($"URL виджета: {message.Extra.Url}");
                
                // Валидация перед показом виджета
                if (string.IsNullOrWhiteSpace(message.Extra.Url))
                {
                    SetConnectionState(ConnectionState.Error, "Ошибка: URL виджета не указан в сообщении");
                    return;
                }

                // Показываем виджет
                try
                {
                    System.Diagnostics.Debug.WriteLine("Создаем окно виджета...");
                    ShowWidget(message.Extra);
                    System.Diagnostics.Debug.WriteLine("Окно виджета создано!");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА создания виджета: {ex}");
                    SetConnectionState(ConnectionState.Error, $"Ошибка создания виджета: {ex.Message}");
                }
            }
            else if (message.Id == 100001)
            {
                System.Diagnostics.Debug.WriteLine("Скрываем виджет");
                // Скрываем виджет
                HideWidget();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Сообщение проигнорировано: ID={message.Id}");
            }
        });
    }

    private void ShowWidget(WidgetExtra config)
    {
        // Закрываем предыдущий виджет, если он есть
        HideWidget();

        // Создаем новое окно виджета
        _widgetWindow = new WidgetWindow(config);
        _widgetWindow.Show();
    }

    private void HideWidget()
    {
        if (_widgetWindow != null)
        {
            _widgetWindow.Close();
            _widgetWindow = null;
        }
    }

    private void SetConnectionState(ConnectionState state, string? errorMessage)
    {
        switch (state)
        {
            case ConnectionState.Connected:
                ConnectButton.Content = "Подключено";
                ConnectButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Зеленый
                ConnectButton.Foreground = System.Windows.Media.Brushes.White;
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                break;

            case ConnectionState.Disconnected:
                ConnectButton.Content = "Подключиться";
                ConnectButton.Background = (SolidColorBrush)System.Windows.Application.Current.Resources["AccentButtonBackground"];
                ConnectButton.Foreground = (SolidColorBrush)System.Windows.Application.Current.Resources["AccentButtonForeground"];
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                break;

            case ConnectionState.Error:
                ConnectButton.Content = "Ошибка";
                ConnectButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Красный
                ConnectButton.Foreground = System.Windows.Media.Brushes.White;
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ErrorTextBlock.Text = errorMessage;
                    ErrorTextBlock.Visibility = Visibility.Visible;
                }
                break;
        }
    }
}
