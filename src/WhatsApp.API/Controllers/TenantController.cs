using Microsoft.AspNetCore.Mvc;
using WhatsApp.API.DTOs;
using WhatsApp.API.Extensions;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant settings
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var tenant = HttpContext.GetTenant();
        if (tenant == null)
        {
            return Unauthorized(new { error = "Tenant not found in context" });
        }

        var tenantDto = new TenantDto
        {
            Id = tenant.Id,
            ClientId = tenant.ClientId,
            Name = tenant.Name,
            Settings = tenant.Settings,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return Ok(tenantDto);
    }

    /// <summary>
    /// Update tenant settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] TenantSettingsDto settingsDto,
        CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.GetTenantId();

        try
        {
            var updatedTenant = await _tenantService.UpdateSettingsAsync(
                tenantId,
                settingsDto.Settings,
                cancellationToken);

            var tenantDto = new TenantDto
            {
                Id = updatedTenant.Id,
                ClientId = updatedTenant.ClientId,
                Name = updatedTenant.Name,
                Settings = updatedTenant.Settings,
                CreatedAt = updatedTenant.CreatedAt,
                UpdatedAt = updatedTenant.UpdatedAt
            };

            return Ok(tenantDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error updating tenant settings");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new tenant (admin only - no authentication required for this endpoint)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantDto createDto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(createDto.ClientId) || string.IsNullOrWhiteSpace(createDto.Name))
        {
            return BadRequest(new { error = "ClientId and Name are required" });
        }

        try
        {
            var tenant = await _tenantService.CreateAsync(
                createDto.ClientId,
                createDto.Name,
                createDto.Settings,
                cancellationToken);

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                ClientId = tenant.ClientId,
                Name = tenant.Name,
                Settings = tenant.Settings,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetSettings),
                new { },
                tenantDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all tenants (admin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);

        var tenantDtos = tenants.Select(t => new TenantDto
        {
            Id = t.Id,
            ClientId = t.ClientId,
            Name = t.Name,
            Settings = t.Settings,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });

        return Ok(tenantDtos);
    }
}