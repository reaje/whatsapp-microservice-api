using Microsoft.EntityFrameworkCore;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Infrastructure.Data.Repositories;

public class SessionRepository : Repository<WhatsAppSession>, ISessionRepository
{
    public SessionRepository(SupabaseContext context) : base(context)
    {
    }

    public async Task<WhatsAppSession?> GetByTenantAndPhoneAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<IEnumerable<WhatsAppSession>> GetActivesessionsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Tenant)
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<WhatsAppSession?> GetLastSessionByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Tenant)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}