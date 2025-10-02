using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;
using WhatsApp.Infrastructure.Data;

namespace WhatsApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly SupabaseContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly ITenantService _tenantService;

    public AuthService(
        SupabaseContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        ITenantService tenantService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<(User User, string Token, int ExpiresIn)> LoginAsync(
        string clientId,
        string email,
        string password)
    {
        _logger.LogInformation("Login attempt for email: {Email}, clientId: {ClientId}", email, clientId);

        // Get tenant
        var tenant = await _tenantService.GetByClientIdAsync(clientId);
        if (tenant == null)
        {
            _logger.LogWarning("Login failed: Invalid tenant clientId: {ClientId}", clientId);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Get user
        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - Email: {Email}", email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Validate password
        if (!ValidatePassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user: {Email}", email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User is inactive - Email: {Email}", email);
            throw new UnauthorizedAccessException("User account is inactive");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate token
        var token = GenerateJwtToken(user, user.Tenant);
        var expiresIn = _configuration.GetValue<int>("Jwt:ExpiresInMinutes", 1440) * 60; // Convert to seconds

        _logger.LogInformation("Login successful for user: {Email}", email);

        return (user, token, expiresIn);
    }

    public async Task<User> RegisterAsync(
        string clientId,
        string email,
        string password,
        string fullName,
        string role)
    {
        _logger.LogInformation("Registration attempt for email: {Email}, clientId: {ClientId}", email, clientId);

        // Get tenant
        var tenant = await _tenantService.GetByClientIdAsync(clientId);
        if (tenant == null)
        {
            _logger.LogWarning("Registration failed: Invalid tenant clientId: {ClientId}", clientId);
            throw new ArgumentException("Invalid client ID");
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: User already exists - Email: {Email}", email);
            throw new InvalidOperationException("User with this email already exists");
        }

        // Validate role
        if (role != "Admin" && role != "User")
        {
            role = "User"; // Default to User if invalid role
        }

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email.ToLower(),
            PasswordHash = HashPassword(password),
            FullName = fullName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Email}", email);

        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string clientId, string email)
    {
        var tenant = await _tenantService.GetByClientIdAsync(clientId);
        if (tenant == null)
            return null;

        return await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email);
    }

    public Task<bool> ValidatePasswordAsync(User user, string password)
    {
        return Task.FromResult(ValidatePassword(password, user.PasswordHash));
    }

    public string GenerateJwtToken(User user, Tenant tenant)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "whatsapp-microservice";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "whatsapp-frontend";
        var expiresInMinutes = _configuration.GetValue<int>("Jwt:ExpiresInMinutes", 1440);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenant.Id.ToString()),
            new Claim("client_id", tenant.ClientId),
            new Claim("role", user.Role),
            new Claim("full_name", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Helper methods for password hashing using BCrypt
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool ValidatePassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
