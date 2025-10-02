using Microsoft.EntityFrameworkCore;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Infrastructure.Data.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(SupabaseContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.ClientId == clientId, cancellationToken);
    }
}