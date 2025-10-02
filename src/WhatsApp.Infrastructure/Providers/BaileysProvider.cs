using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;
using WhatsApp.Core.Models;

namespace WhatsApp.Infrastructure.Providers;

/// <summary>
/// Baileys WhatsApp provider implementation with HTTP communication to Node.js service
/// </summary>
public class BaileysProvider : IWhatsAppProvider
{
    private readonly ILogger<BaileysProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baileysServiceUrl;
    private SessionStatus _currentStatus;
    private string? _phoneNumber;
    private string? _sessionId;

    public event EventHandler<IncomingMessage>? OnMessageReceived;
    public event EventHandler<SessionStatus>? OnStatusChanged;

    public BaileysProvider(
        ILogger<BaileysProvider> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("BaileysService");
        _baileysServiceUrl = configuration["BaileysService:Url"] ?? "http://localhost:3000";

        _currentStatus = new SessionStatus
        {
            IsConnected = false,
            Status = "disconnected"
        };
    }

    public async Task<SessionStatus> InitializeAsync(string phoneNumber, TenantConfig config, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Baileys session for phone: {PhoneNumber}, Tenant: {TenantId}",
            phoneNumber, config.TenantId);

        try
        {
            _phoneNumber = phoneNumber;
            _sessionId = $"session-{config.TenantId}-{phoneNumber.Replace("+", "")}";

            var request = new
            {
                sessionId = _sessionId,
                phoneNumber = phoneNumber
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baileysServiceUrl}/api/sessions/initialize", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to initialize Baileys session. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return new SessionStatus
                {
                    IsConnected = false,
                    Status = "failed",
                    PhoneNumber = phoneNumber,
                    Metadata = new Dictionary<string, object>
                    {
                        { "error", errorContent }
                    }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var status = result?.RootElement.GetProperty("status").GetString() ?? "unknown";
            var qrCode = result?.RootElement.TryGetProperty("qrCode", out var qr) == true ? qr.GetString() : null;

            // Always fetch latest status to ensure we have the most up-to-date QR code
            // Retry with delays to wait for QR code generation (takes ~300-500ms)
            if (status != "connected" && status != "already_connected")
            {
                _logger.LogInformation("Fetching latest session status from Baileys with retry logic");

                int maxRetries = 3;
                int delayMs = 500;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        // Add delay on retry attempts to wait for QR code generation
                        if (attempt > 1)
                        {
                            _logger.LogInformation("Waiting {Delay}ms before retry attempt {Attempt}/{MaxRetries}", delayMs, attempt, maxRetries);
                            await Task.Delay(delayMs, cancellationToken);
                        }

                        var statusResponse = await _httpClient.GetAsync($"{_baileysServiceUrl}/api/sessions/{_sessionId}/status", cancellationToken);

                        if (statusResponse.IsSuccessStatusCode)
                        {
                            var statusContent = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogInformation("Status response (attempt {Attempt}): {StatusContent}", attempt, statusContent);

                            var statusResult = JsonDocument.Parse(statusContent);

                            // Try to get QR code
                            if (statusResult?.RootElement.TryGetProperty("qrCode", out var statusQr) == true)
                            {
                                var qrValue = statusQr.GetString();
                                if (!string.IsNullOrWhiteSpace(qrValue))
                                {
                                    qrCode = qrValue;
                                    _logger.LogInformation("QR Code found in status response, length: {Length}", qrCode.Length);
                                }
                            }

                            // Update status from status endpoint
                            if (statusResult?.RootElement.TryGetProperty("status", out var statusProp) == true)
                            {
                                var statusFromEndpoint = statusProp.GetString();
                                if (!string.IsNullOrEmpty(statusFromEndpoint))
                                {
                                    status = statusFromEndpoint;
                                    _logger.LogInformation("Updated status from endpoint: {Status}", status);
                                }
                            }

                            // If we got a QR code or status is connected, we're done
                            if (!string.IsNullOrEmpty(qrCode) || status == "connected" || status == "already_connected")
                            {
                                _logger.LogInformation("Successfully retrieved session status with QR code: {HasQR}", !string.IsNullOrEmpty(qrCode));
                                break;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to get status from Baileys. Status code: {StatusCode}", statusResponse.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching status from Baileys (attempt {Attempt})", attempt);
                    }
                }
            }

            _currentStatus = new SessionStatus
            {
                IsConnected = status == "connected" || status == "already_connected",
                PhoneNumber = phoneNumber,
                Status = status,
                ConnectedAt = status == "connected" ? DateTime.UtcNow : null,
                Metadata = new Dictionary<string, object>
                {
                    { "provider", "baileys" },
                    { "sessionId", _sessionId },
                    { "tenantId", config.TenantId }
                }
            };

            if (qrCode != null)
            {
                _currentStatus.Metadata["qrCode"] = qrCode;
                _logger.LogInformation("QR Code added to session status");
            }

            OnStatusChanged?.Invoke(this, _currentStatus);

            _logger.LogInformation("Baileys session initialized. Status: {Status}, HasQR: {HasQR}", status, qrCode != null);

            return _currentStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Baileys session");
            return new SessionStatus
            {
                IsConnected = false,
                Status = "error",
                PhoneNumber = phoneNumber,
                Metadata = new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }
            };
        }
    }

    public async Task<MessageResult> SendTextAsync(string to, string content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending text message via Baileys to: {To}", to);

        if (string.IsNullOrEmpty(_sessionId))
        {
            _logger.LogError("Cannot send message: session not initialized");
            return CreateFailedResult("Session not initialized");
        }

        try
        {
            // Remove '+' from phone number for Baileys compatibility
            var formattedTo = to.TrimStart('+');

            var request = new
            {
                sessionId = _sessionId,
                to = formattedTo,
                content = content
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baileysServiceUrl}/api/messages/text", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send text message. Error: {Error}", errorContent);
                return CreateFailedResult(errorContent);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var messageId = result?.RootElement.GetProperty("messageId").GetString() ?? string.Empty;

            _logger.LogInformation("Text message sent successfully via Baileys: {MessageId}", messageId);

            return new MessageResult
            {
                MessageId = messageId,
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "to", to },
                    { "contentLength", content.Length }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text message");
            return CreateFailedResult(ex.Message);
        }
    }

    public async Task<MessageResult> SendMediaAsync(string to, byte[] media, MessageType mediaType, string? caption = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending media message via Baileys to: {To}, Type: {MediaType}", to, mediaType);

        if (string.IsNullOrEmpty(_sessionId))
        {
            return CreateFailedResult("Session not initialized");
        }

        try
        {
            // Remove '+' from phone number for Baileys compatibility
            var formattedTo = to.TrimStart('+');

            var mediaBase64 = Convert.ToBase64String(media);
            var mediaTypeString = mediaType.ToString().ToLower();

            var request = new
            {
                sessionId = _sessionId,
                to = formattedTo,
                mediaBase64 = mediaBase64,
                mediaType = mediaTypeString,
                caption = caption
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baileysServiceUrl}/api/messages/media", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return CreateFailedResult(errorContent);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var messageId = result?.RootElement.GetProperty("messageId").GetString() ?? string.Empty;

            return new MessageResult
            {
                MessageId = messageId,
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "to", to },
                    { "mediaType", mediaType.ToString() },
                    { "mediaSize", media.Length }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending media message");
            return CreateFailedResult(ex.Message);
        }
    }

    public async Task<MessageResult> SendLocationAsync(string to, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending location via Baileys to: {To}, Lat: {Lat}, Lng: {Lng}",
            to, latitude, longitude);

        if (string.IsNullOrEmpty(_sessionId))
        {
            return CreateFailedResult("Session not initialized");
        }

        try
        {
            // Remove '+' from phone number for Baileys compatibility
            var formattedTo = to.TrimStart('+');

            var request = new
            {
                sessionId = _sessionId,
                to = formattedTo,
                latitude = latitude,
                longitude = longitude
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baileysServiceUrl}/api/messages/location", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return CreateFailedResult(errorContent);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var messageId = result?.RootElement.GetProperty("messageId").GetString() ?? string.Empty;

            return new MessageResult
            {
                MessageId = messageId,
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "to", to },
                    { "latitude", latitude },
                    { "longitude", longitude }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending location");
            return CreateFailedResult(ex.Message);
        }
    }

    public async Task<MessageResult> SendAudioAsync(string to, byte[] audio, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending audio via Baileys to: {To}, Size: {Size} bytes", to, audio.Length);

        if (string.IsNullOrEmpty(_sessionId))
        {
            return CreateFailedResult("Session not initialized");
        }

        try
        {
            // Remove '+' from phone number for Baileys compatibility
            var formattedTo = to.TrimStart('+');

            var audioBase64 = Convert.ToBase64String(audio);

            var request = new
            {
                sessionId = _sessionId,
                to = formattedTo,
                audioBase64 = audioBase64
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baileysServiceUrl}/api/messages/audio", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return CreateFailedResult(errorContent);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var messageId = result?.RootElement.GetProperty("messageId").GetString() ?? string.Empty;

            return new MessageResult
            {
                MessageId = messageId,
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "to", to },
                    { "audioSize", audio.Length }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio");
            return CreateFailedResult(ex.Message);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting Baileys session");

        if (string.IsNullOrEmpty(_sessionId))
        {
            return;
        }

        try
        {
            var response = await _httpClient.DeleteAsync($"{_baileysServiceUrl}/api/sessions/{_sessionId}", cancellationToken);

            _currentStatus = new SessionStatus
            {
                IsConnected = false,
                Status = "disconnected",
                PhoneNumber = _phoneNumber
            };

            OnStatusChanged?.Invoke(this, _currentStatus);

            _logger.LogInformation("Baileys session disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Baileys session");
        }
    }

    public async Task<SessionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            return _currentStatus;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{_baileysServiceUrl}/api/sessions/{_sessionId}/status", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return _currentStatus;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            var status = result?.RootElement.GetProperty("status").GetString() ?? "unknown";

            _currentStatus.Status = status;
            _currentStatus.IsConnected = status == "connected";

            return _currentStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session status");
            return _currentStatus;
        }
    }

    private MessageResult CreateFailedResult(string error)
    {
        return new MessageResult
        {
            MessageId = string.Empty,
            Status = MessageStatus.Failed,
            Provider = "baileys",
            Timestamp = DateTime.UtcNow,
            Error = error
        };
    }
}