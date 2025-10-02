using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsApp.API.DTOs.User;
using WhatsApp.API.Extensions;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (Admin only, or User for their own tenant)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userRole = HttpContext.GetUserRole();
        var tenantId = HttpContext.GetTenantId();

        IEnumerable<Core.Entities.User> users;

        // Admins can see all users in their tenant, Users can only see users in their tenant
        if (userRole == "Admin")
        {
            users = await _userService.GetAllByTenantIdAsync(tenantId, cancellationToken);
        }
        else
        {
            users = await _userService.GetAllByTenantIdAsync(tenantId, cancellationToken);
        }

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            TenantId = u.TenantId,
            TenantName = u.Tenant?.Name ?? string.Empty,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        });

        return Ok(userDtos);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Check if user belongs to the same tenant
        var tenantId = HttpContext.GetTenantId();
        if (user.TenantId != tenantId)
        {
            return Forbid();
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name ?? string.Empty,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Create a new user (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDto createDto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tenantId = HttpContext.GetTenantId();

        try
        {
            var user = await _userService.CreateAsync(
                tenantId,
                createDto.Email,
                createDto.Password,
                createDto.FullName,
                createDto.Role,
                cancellationToken);

            var userDto = new UserDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                TenantName = user.Tenant?.Name ?? string.Empty,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                userDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update user (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserDto updateDto,
        CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.GetTenantId();

        // Check if user exists and belongs to the same tenant
        var existingUser = await _userService.GetByIdAsync(id, cancellationToken);
        if (existingUser == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (existingUser.TenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            var user = await _userService.UpdateAsync(
                id,
                updateDto.FullName,
                updateDto.Role,
                updateDto.IsActive,
                cancellationToken);

            var userDto = new UserDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                TenantName = user.Tenant?.Name ?? string.Empty,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error updating user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update user password (Admin only)
    /// </summary>
    [HttpPut("{id:guid}/password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePassword(
        Guid id,
        [FromBody] UpdatePasswordDto updatePasswordDto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tenantId = HttpContext.GetTenantId();

        // Check if user exists and belongs to the same tenant
        var existingUser = await _userService.GetByIdAsync(id, cancellationToken);
        if (existingUser == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (existingUser.TenantId != tenantId)
        {
            return Forbid();
        }

        try
        {
            await _userService.UpdatePasswordAsync(id, updatePasswordDto.NewPassword, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error updating user password");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate user (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.GetTenantId();

        // Check if user exists and belongs to the same tenant
        var existingUser = await _userService.GetByIdAsync(id, cancellationToken);
        if (existingUser == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (existingUser.TenantId != tenantId)
        {
            return Forbid();
        }

        var result = await _userService.DeactivateAsync(id, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "User not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete user (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = HttpContext.GetTenantId();

        // Check if user exists and belongs to the same tenant
        var existingUser = await _userService.GetByIdAsync(id, cancellationToken);
        if (existingUser == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (existingUser.TenantId != tenantId)
        {
            return Forbid();
        }

        var result = await _userService.DeleteAsync(id, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "User not found" });
        }

        return NoContent();
    }
}
