using System.ComponentModel.DataAnnotations;
using WhatsApp.Core.Enums;

namespace WhatsApp.API.DTOs;

public class SendMediaMessageRequest
{
    [Required]
    [Phone]
    public string To { get; set; } = string.Empty;

    [Required]
    public string MediaBase64 { get; set; } = string.Empty;

    [Required]
    public MessageType MediaType { get; set; }

    [StringLength(1024)]
    public string? Caption { get; set; }
}