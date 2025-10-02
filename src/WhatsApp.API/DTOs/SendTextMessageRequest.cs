using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs;

public class SendTextMessageRequest
{
    [Required]
    [Phone]
    public string To { get; set; } = string.Empty;

    [Required]
    [StringLength(4096, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}