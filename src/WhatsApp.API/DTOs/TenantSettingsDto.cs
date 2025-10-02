using System.Text.Json;

namespace WhatsApp.API.DTOs;

public class TenantSettingsDto
{
    public JsonDocument Settings { get; set; } = JsonDocument.Parse("{}");
}

public class TenantDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTenantDto
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Settings { get; set; }
}