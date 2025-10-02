using WhatsApp.Core.Enums;

namespace WhatsApp.API.DTOs;

public class IncomingMessageWebhookDto
{
    public string MessageId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public string? TextContent { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaMimeType { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}