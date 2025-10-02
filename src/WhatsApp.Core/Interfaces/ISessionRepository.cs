using WhatsApp.Core.Entities;

namespace WhatsApp.Core.Interfaces;

public interface ISessionRepository : IRepository<WhatsAppSession>
{
    Task<WhatsAppSession?> GetByTenantAndPhoneAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<WhatsAppSession>> GetActivesessionsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<WhatsAppSession?> GetLastSessionByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}