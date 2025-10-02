using Microsoft.Extensions.Logging;
using System.Text.Json;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantRepository tenantRepository, ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<Tenant?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tenant by client ID: {ClientId}", clientId);

        var tenant = await _tenantRepository.GetByClientIdAsync(clientId, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for client ID: {ClientId}", clientId);
        }

        return tenant;
    }

    public async Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting tenant by ID: {TenantId}", tenantId);

        return await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
    }

    public async Task<Tenant> CreateAsync(string clientId, string name, JsonDocument? settings = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new tenant: {ClientId} - {Name}", clientId, name);

        // Check if tenant already exists
        var existingTenant = await _tenantRepository.GetByClientIdAsync(clientId, cancellationToken);
        if (existingTenant != null)
        {
            _logger.LogError("Tenant already exists with client ID: {ClientId}", clientId);
            throw new InvalidOperationException($"Tenant with client ID '{clientId}' already exists");
        }

        var tenant = new Tenant
        {
            ClientId = clientId,
            Name = name,
            Settings = settings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdTenant = await _tenantRepository.AddAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant created successfully: {TenantId} - {ClientId}", createdTenant.Id, createdTenant.ClientId);

        return createdTenant;
    }

    public async Task<Tenant> UpdateSettingsAsync(Guid tenantId, JsonDocument settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating settings for tenant: {TenantId}", tenantId);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            _logger.LogError("Tenant not found: {TenantId}", tenantId);
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");
        }

        tenant.Settings = settings;
        tenant.UpdatedAt = DateTime.UtcNow;

        var updatedTenant = await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        _logger.LogInformation("Tenant settings updated successfully: {TenantId}", tenantId);

        return updatedTenant;
    }

    public async Task<bool> ValidateTenantAsync(string clientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating tenant: {ClientId}", clientId);

        var tenant = await _tenantRepository.GetByClientIdAsync(clientId, cancellationToken);

        return tenant != null;
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tenants");

        return await _tenantRepository.GetAllAsync(cancellationToken);
    }
}