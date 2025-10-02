using Microsoft.Extensions.Logging;
using System.Text.Json;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;
using WhatsApp.Core.Models;

namespace WhatsApp.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IWhatsAppProvider _whatsAppProvider;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ISessionRepository sessionRepository,
        IWhatsAppProvider whatsAppProvider,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _whatsAppProvider = whatsAppProvider;
        _logger = logger;
    }

    /// <summary>
    /// Normalizes phone number by removing '+' and any non-numeric characters
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return phoneNumber.Replace("+", "").Trim();
    }

    public async Task<SessionStatus> InitializeSessionAsync(Guid tenantId, string phoneNumber, ProviderType providerType, CancellationToken cancellationToken = default)
    {
        // Normalize phone number (remove '+' and whitespaces)
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        _logger.LogInformation("Initializing session for tenant {TenantId}, phone {PhoneNumber} (normalized: {NormalizedPhone}), provider {Provider}",
            tenantId, phoneNumber, normalizedPhone, providerType);

        // Check if session already exists
        var existingSession = await _sessionRepository.GetByTenantAndPhoneAsync(tenantId, normalizedPhone, cancellationToken);

        if (existingSession != null)
        {
            _logger.LogInformation("Found existing session for phone {PhoneNumber}. Deleting old session before creating new one.", normalizedPhone);

            try
            {
                // Try to disconnect from provider if session is active
                if (existingSession.IsActive)
                {
                    try
                    {
                        await _whatsAppProvider.DisconnectAsync(cancellationToken);
                        _logger.LogInformation("Disconnected existing active session for phone {PhoneNumber}", normalizedPhone);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to disconnect existing session, continuing with deletion");
                    }
                }

                // Delete old session from database
                await _sessionRepository.DeleteAsync(existingSession, cancellationToken);
                _logger.LogInformation("Deleted existing session for phone {PhoneNumber}", normalizedPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting existing session for phone {PhoneNumber}", normalizedPhone);
                throw;
            }
        }

        // Create tenant config
        var config = new TenantConfig
        {
            TenantId = tenantId,
            PreferredProvider = providerType,
            ClientId = $"tenant-{tenantId}"
        };

        // Initialize provider
        var status = await _whatsAppProvider.InitializeAsync(normalizedPhone, config, cancellationToken);

        // Create new session in database
        var newSession = new WhatsAppSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = normalizedPhone,
            ProviderType = providerType,
            IsActive = status.IsConnected,
            SessionData = JsonDocument.Parse(JsonSerializer.Serialize(status.Metadata ?? new Dictionary<string, object>())),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _sessionRepository.AddAsync(newSession, cancellationToken);

        _logger.LogInformation("New session created successfully for phone {PhoneNumber}", normalizedPhone);

        return status;
    }

    public async Task<SessionStatus> GetSessionStatusAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation("Getting session status for tenant {TenantId}, phone {PhoneNumber} (normalized: {NormalizedPhone})", tenantId, phoneNumber, normalizedPhone);

        var session = await _sessionRepository.GetByTenantAndPhoneAsync(tenantId, normalizedPhone, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session not found for tenant {TenantId}, phone {PhoneNumber}", tenantId, phoneNumber);
            return new SessionStatus
            {
                IsConnected = false,
                Status = "not_found",
                PhoneNumber = phoneNumber
            };
        }

        // Get real-time status from provider
        var providerStatus = await _whatsAppProvider.GetStatusAsync(cancellationToken);

        // Update session if status changed
        if (session.IsActive != providerStatus.IsConnected)
        {
            session.IsActive = providerStatus.IsConnected;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        return providerStatus;
    }

    public async Task<IEnumerable<WhatsAppSession>> GetTenantSessionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all sessions for tenant {TenantId}", tenantId);

        var sessions = await _sessionRepository.GetActivesessionsByTenantAsync(tenantId, cancellationToken);
        return sessions;
    }

    public async Task<bool> DisconnectSessionAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation("Disconnecting session for tenant {TenantId}, phone {PhoneNumber} (normalized: {NormalizedPhone})", tenantId, phoneNumber, normalizedPhone);

        var session = await _sessionRepository.GetByTenantAndPhoneAsync(tenantId, normalizedPhone, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session not found for tenant {TenantId}, phone {PhoneNumber}", tenantId, phoneNumber);
            return false;
        }

        // Disconnect from provider
        await _whatsAppProvider.DisconnectAsync(cancellationToken);

        // Update session status
        session.IsActive = false;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Session disconnected successfully for phone {PhoneNumber}", phoneNumber);

        return true;
    }

    public async Task<string?> GetQRCodeAsync(Guid tenantId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation("Getting QR code for tenant {TenantId}, phone {PhoneNumber} (normalized: {NormalizedPhone})", tenantId, phoneNumber, normalizedPhone);

        var session = await _sessionRepository.GetByTenantAndPhoneAsync(tenantId, normalizedPhone, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session not found for tenant {TenantId}, phone {PhoneNumber}", tenantId, phoneNumber);
            return null;
        }

        // TODO: Implement QR code generation for Baileys provider
        // For MVP, return placeholder
        return "QR_CODE_PLACEHOLDER_BASE64";
    }
}