using WhatsApp.Core.Entities;

namespace WhatsApp.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User> CreateAsync(Guid tenantId, string email, string password, string fullName, string role, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(Guid id, string? fullName, string? role, bool? isActive, CancellationToken cancellationToken = default);
    Task<bool> UpdatePasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
