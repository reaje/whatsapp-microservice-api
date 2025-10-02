using WhatsApp.Core.Enums;

namespace WhatsApp.Core.Models;

public class TenantConfig
{
    public Guid TenantId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public ProviderType PreferredProvider { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}