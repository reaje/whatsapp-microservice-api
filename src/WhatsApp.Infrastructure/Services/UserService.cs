using Microsoft.Extensions.Logging;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user by ID: {UserId}", id);
        return await _userRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all users for tenant: {TenantId}", tenantId);
        return await _userRepository.GetAllByTenantIdAsync(tenantId, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all users");
        return await _userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(
        Guid tenantId,
        string email,
        string password,
        string fullName,
        string role,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new user: {Email}", email);

        // Check if user already exists
        if (await _userRepository.ExistsAsync(email, cancellationToken))
        {
            _logger.LogError("User already exists with email: {Email}", email);
            throw new InvalidOperationException($"User with email '{email}' already exists");
        }

        // Validate role
        if (role != "Admin" && role != "User")
        {
            _logger.LogError("Invalid role: {Role}", role);
            throw new InvalidOperationException($"Invalid role '{role}'. Must be 'Admin' or 'User'");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("User created successfully: {UserId} - {Email}", createdUser.Id, createdUser.Email);

        return createdUser;
    }

    public async Task<User> UpdateAsync(
        Guid id,
        string? fullName,
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User not found: {UserId}", id);
            throw new InvalidOperationException($"User with ID '{id}' not found");
        }

        // Update fields if provided
        if (fullName != null)
        {
            user.FullName = fullName;
        }

        if (role != null)
        {
            // Validate role
            if (role != "Admin" && role != "User")
            {
                _logger.LogError("Invalid role: {Role}", role);
                throw new InvalidOperationException($"Invalid role '{role}'. Must be 'Admin' or 'User'");
            }
            user.Role = role;
        }

        if (isActive.HasValue)
        {
            user.IsActive = isActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User updated successfully: {UserId}", updatedUser.Id);

        return updatedUser;
    }

    public async Task<bool> UpdatePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating password for user: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User not found: {UserId}", id);
            throw new InvalidOperationException($"User with ID '{id}' not found");
        }

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password updated successfully for user: {UserId}", id);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user: {UserId}", id);

        var result = await _userRepository.DeleteAsync(id, cancellationToken);

        if (result)
        {
            _logger.LogInformation("User deleted successfully: {UserId}", id);
        }
        else
        {
            _logger.LogWarning("User not found for deletion: {UserId}", id);
        }

        return result;
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating user: {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User not found: {UserId}", id);
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User deactivated successfully: {UserId}", id);

        return true;
    }
}
