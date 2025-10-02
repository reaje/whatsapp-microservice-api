using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs.User;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";
}
