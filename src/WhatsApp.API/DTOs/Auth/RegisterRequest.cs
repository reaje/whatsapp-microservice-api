using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Client ID is required")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // "Admin" or "User"
}
