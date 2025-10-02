using System.Text.Json;

namespace WhatsApp.Core.Entities;

public class AIConversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AgentId { get; set; }
    public Guid SessionId { get; set; }
    public JsonDocument? Context { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public AIAgent Agent { get; set; } = null!;
    public WhatsAppSession Session { get; set; } = null!;
}