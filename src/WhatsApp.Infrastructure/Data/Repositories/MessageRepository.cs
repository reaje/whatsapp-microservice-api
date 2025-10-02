using Microsoft.EntityFrameworkCore;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Infrastructure.Data.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(SupabaseContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetByTenantAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Session)
            .Include(m => m.Tenant)
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetBySessionAsync(Guid sessionId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Session)
            .Include(m => m.Tenant)
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Message?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Session)
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetByPhoneNumberAsync(Guid tenantId, string phoneNumber, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Session)
            .Include(m => m.Tenant)
            .Where(m => m.TenantId == tenantId && (m.FromNumber == phoneNumber || m.ToNumber == phoneNumber))
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Message>> GetLatestMessagePerContactAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var messages = await _dbSet
            .Include(m => m.Session)
            .Include(m => m.Tenant)
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        // Group by phone number (either from or to, excluding self)
        var conversationDict = new Dictionary<string, Message>();

        foreach (var message in messages)
        {
            // Get the contact phone number (the one that's not the session's phone)
            var contactPhone = message.FromNumber != message.Session?.PhoneNumber
                ? message.FromNumber
                : message.ToNumber;

            if (!conversationDict.ContainsKey(contactPhone) ||
                message.CreatedAt > conversationDict[contactPhone].CreatedAt)
            {
                conversationDict[contactPhone] = message;
            }
        }

        return conversationDict;
    }
}