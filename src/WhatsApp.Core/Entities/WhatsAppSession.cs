using System.Text.Json;
using WhatsApp.Core.Enums;

namespace WhatsApp.Core.Entities;

public class WhatsAppSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public ProviderType ProviderType { get; set; }
    public JsonDocument? SessionData { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<AIConversation> AIConversations { get; set; } = new List<AIConversation>();
}