using System.Text.Json;
using WhatsApp.Core.Enums;

namespace WhatsApp.Core.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public string? MessageId { get; set; }
    public string FromNumber { get; set; } = string.Empty;
    public string ToNumber { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public JsonDocument? Content { get; set; }
    public MessageStatus Status { get; set; }
    public bool AiProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public WhatsAppSession Session { get; set; } = null!;
}