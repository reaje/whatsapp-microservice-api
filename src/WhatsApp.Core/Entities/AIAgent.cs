using System.Text.Json;

namespace WhatsApp.Core.Entities;

public class AIAgent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public JsonDocument? Configuration { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<AIConversation> Conversations { get; set; } = new List<AIConversation>();
}