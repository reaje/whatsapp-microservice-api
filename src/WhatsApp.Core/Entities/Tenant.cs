using System.Text.Json;

namespace WhatsApp.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<WhatsAppSession> Sessions { get; set; } = new List<WhatsAppSession>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<AIAgent> AIAgents { get; set; } = new List<AIAgent>();
}