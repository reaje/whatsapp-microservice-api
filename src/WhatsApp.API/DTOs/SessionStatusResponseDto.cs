namespace WhatsApp.API.DTOs;

public class SessionStatusResponseDto
{
    public bool IsConnected { get; set; }
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ConnectedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string Provider { get; set; } = "baileys";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
