using WhatsApp.Core.Enums;

namespace WhatsApp.API.DTOs;

public class MessageStatusUpdateWebhookDto
{
    public string MessageId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
}