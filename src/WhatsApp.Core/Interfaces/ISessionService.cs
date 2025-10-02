using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Models;

namespace WhatsApp.Core.Interfaces;

public interface ISessionService
{
    /// <summary>
    /// Initialize a new WhatsApp session
    /// </summary>
    Task<SessionStatus> InitializeSessionAsync(Guid tenantId, string phoneNumber, ProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session status
    /// </summary>
    Task<SessionStatus> GetSessionStatusAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all sessions for a tenant
    /// </summary>
    Task<IEnumerable<WhatsAppSession>> GetTenantSessionsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect a session
    /// </summary>
    Task<bool> DisconnectSessionAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get QR code for session authentication (Baileys only)
    /// </summary>
    Task<string?> GetQRCodeAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default);
}