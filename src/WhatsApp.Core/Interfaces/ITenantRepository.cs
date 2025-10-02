using WhatsApp.Core.Entities;

namespace WhatsApp.Core.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}