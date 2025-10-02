namespace WhatsApp.Core.Models;

public class SessionStatus
{
    public bool IsConnected { get; set; }
    public string? PhoneNumber { get; set; }
    public string? QrCode { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public string Status { get; set; } = "disconnected";
    public Dictionary<string, object>? Metadata { get; set; }
}