using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs.User;

public class UpdatePasswordDto
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
