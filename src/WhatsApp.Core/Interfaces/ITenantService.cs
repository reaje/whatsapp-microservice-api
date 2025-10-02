using WhatsApp.Core.Entities;
using System.Text.Json;

namespace WhatsApp.Core.Interfaces;

public interface ITenantService
{
    Task<Tenant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Tenant> CreateAsync(string clientId, string name, JsonDocument? settings = null, CancellationToken cancellationToken = default);
    Task<Tenant> UpdateSettingsAsync(Guid tenantId, JsonDocument settings, CancellationToken cancellationToken = default);
    Task<bool> ValidateTenantAsync(string clientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
}