using System.IO;
using System.Net.Http;
using System.Text.Json;
using FixWidget.Models;

namespace FixWidget.Services;

/// <summary>
/// Service for connecting to ntfy server via Server-Sent Events (SSE).
/// Parses incoming messages and raises events for widget display/hide commands.
/// </summary>
public class NtfyService : IDisposable
{
    private static readonly HttpClient _sharedHttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isConnected;
    private string? _lastNtfyUrl;

    public event EventHandler<NtfyMessage>? MessageReceived;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public bool IsConnected => _isConnected;

    public async Task ConnectAsync(string ntfyUrl)
    {
        if (_isConnected)
        {
            throw new InvalidOperationException("Already connected to ntfy");
        }

        _lastNtfyUrl = ntfyUrl;
        await ConnectInternalAsync(ntfyUrl);
    }

    private async Task ConnectInternalAsync(string ntfyUrl)
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var url = ntfyUrl.TrimEnd('/') + "/json";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "text/event-stream");
            request.Headers.Add("Connection", "keep-alive");

            var response = await _sharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Connection error: {response.StatusCode}");
            }

            _isConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);

            await using var stream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);
            using var reader = new StreamReader(stream);

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                
                if (line == null)
                {
                    System.Diagnostics.Debug.WriteLine("Connection closed (ReadLine returned null)");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                System.Diagnostics.Debug.WriteLine($"Received line: {line}");

                try
                {
                    // Step 1: Parse ntfy envelope wrapper
                    var envelope = JsonSerializer.Deserialize<NtfyEnvelope>(line, _jsonOptions);
                    
                    if (envelope == null || envelope.Event != "message" || string.IsNullOrWhiteSpace(envelope.Message))
                    {
                        // Keepalive or other service message
                        continue;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"📧 Ntfy envelope: event={envelope.Event}, message length={envelope.Message.Length}");
                    
                    // Step 2: Parse nested JSON from message field
                    NtfyMessage? message = null;
                    
                    if (envelope.Message.TrimStart().StartsWith("["))
                    {
                        System.Diagnostics.Debug.WriteLine("📦 Parsing message as array");
                        var messages = JsonSerializer.Deserialize<List<NtfyMessage>>(envelope.Message, _jsonOptions);
                        message = messages?.FirstOrDefault();
                        System.Diagnostics.Debug.WriteLine($"📦 Array contains {messages?.Count ?? 0} elements");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("📄 Parsing message as single object");
                        message = JsonSerializer.Deserialize<NtfyMessage>(envelope.Message, _jsonOptions);
                    }
                    
                    if (message != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Parsed: ID={message.Id}, CreatedAt={message.CreatedAt}, Extra={message.Extra != null}");
                        
                        // Process only ID 100000 (show widget) or 100001 (hide widget)
                        if (message.Id == 100000 || message.Id == 100001)
                        {
                            System.Diagnostics.Debug.WriteLine($"✓ Message ID={message.Id}");
                            
                            if (message.Extra != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"  URL: {message.Extra.Url}");
                                System.Diagnostics.Debug.WriteLine($"  Scale: {message.Extra.Scale}, Opacity: {message.Extra.Opacity}");
                                System.Diagnostics.Debug.WriteLine($"  Position: setupX={message.Extra.SetupX}, setupY={message.Extra.SetupY}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Extra is NULL!");
                            }
                            
                            MessageReceived?.Invoke(this, message);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⏭️ Ignoring ID={message.Id} (not 100000/100001)");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ message parsing returned null");
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                    continue;
                }
            }

            // Reconnect if connection was lost (not cancelled)
            if (!_cancellationTokenSource.Token.IsCancellationRequested && _isConnected)
            {
                System.Diagnostics.Debug.WriteLine("Connection lost, reconnecting in 2 seconds...");
                _isConnected = false;
                
                await Task.Delay(2000, _cancellationTokenSource.Token);
                
                if (!_cancellationTokenSource.Token.IsCancellationRequested && _lastNtfyUrl != null)
                {
                    System.Diagnostics.Debug.WriteLine("Reconnecting...");
                    await ConnectInternalAsync(_lastNtfyUrl);
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Connection cancelled by user");
            _isConnected = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
            _isConnected = false;
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public void Disconnect()
    {
        if (!_isConnected)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _isConnected = false;
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
