using WhatsApp.Core.Entities;

namespace WhatsApp.Core.Interfaces;

public interface IAuthService
{
    Task<(User User, string Token, int ExpiresIn)> LoginAsync(string clientId, string email, string password);
    Task<User> RegisterAsync(string clientId, string email, string password, string fullName, string role);
    Task<User?> GetUserByEmailAsync(string clientId, string email);
    Task<bool> ValidatePasswordAsync(User user, string password);
    string GenerateJwtToken(User user, Tenant tenant);
}
