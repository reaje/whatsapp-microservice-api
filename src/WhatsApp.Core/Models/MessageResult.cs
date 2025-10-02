using WhatsApp.Core.Enums;

namespace WhatsApp.Core.Models;

public class MessageResult
{
    public string MessageId { get; set; } = string.Empty;
    public MessageStatus Status { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}