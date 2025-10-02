using WhatsApp.Core.Entities;

namespace WhatsApp.Core.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByTenantAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetBySessionAsync(Guid sessionId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<Message?> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetByPhoneNumberAsync(Guid tenantId, string phoneNumber, int limit = 50, CancellationToken cancellationToken = default);
    Task<Dictionary<string, Message>> GetLatestMessagePerContactAsync(Guid tenantId, CancellationToken cancellationToken = default);
}