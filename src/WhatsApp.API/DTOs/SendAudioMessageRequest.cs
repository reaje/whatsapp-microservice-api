using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs;

public class SendAudioMessageRequest
{
    [Required]
    [Phone]
    public string To { get; set; } = string.Empty;

    [Required]
    public string AudioBase64 { get; set; } = string.Empty;
}