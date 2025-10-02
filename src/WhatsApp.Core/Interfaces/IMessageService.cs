using WhatsApp.Core.Enums;
using WhatsApp.Core.Models;

namespace WhatsApp.Core.Interfaces;

public interface IMessageService
{
    /// <summary>
    /// Send a text message via WhatsApp
    /// </summary>
    Task<MessageResult> SendTextAsync(Guid tenantId, string to, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a media message (image, video, document)
    /// </summary>
    Task<MessageResult> SendMediaAsync(Guid tenantId, string to, byte[] media, MessageType mediaType, string? caption = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a location message
    /// </summary>
    Task<MessageResult> SendLocationAsync(Guid tenantId, string to, double latitude, double longitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an audio message
    /// </summary>
    Task<MessageResult> SendAudioAsync(Guid tenantId, string to, byte[] audio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message status by message ID
    /// </summary>
    Task<MessageResult?> GetMessageStatusAsync(Guid tenantId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message history for a specific phone number (conversation)
    /// </summary>
    Task<IEnumerable<object>> GetMessageHistoryAsync(Guid tenantId, string phoneNumber, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conversations (contacts with messages) for a tenant
    /// </summary>
    Task<IEnumerable<object>> GetConversationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}