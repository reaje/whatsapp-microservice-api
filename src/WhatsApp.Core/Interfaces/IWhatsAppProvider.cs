using WhatsApp.Core.Enums;
using WhatsApp.Core.Models;

namespace WhatsApp.Core.Interfaces;

public interface IWhatsAppProvider
{
    /// <summary>
    /// Initialize WhatsApp session for a phone number
    /// </summary>
    Task<SessionStatus> InitializeAsync(string phoneNumber, TenantConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send text message
    /// </summary>
    Task<MessageResult> SendTextAsync(string to, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send media message (image, video, document)
    /// </summary>
    Task<MessageResult> SendMediaAsync(string to, byte[] media, MessageType mediaType, string? caption = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send location
    /// </summary>
    Task<MessageResult> SendLocationAsync(string to, double latitude, double longitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send audio message
    /// </summary>
    Task<MessageResult> SendAudioAsync(string to, byte[] audio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect session
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current session status
    /// </summary>
    Task<SessionStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when a message is received
    /// </summary>
    event EventHandler<IncomingMessage>? OnMessageReceived;

    /// <summary>
    /// Event fired when session status changes
    /// </summary>
    event EventHandler<SessionStatus>? OnStatusChanged;
}