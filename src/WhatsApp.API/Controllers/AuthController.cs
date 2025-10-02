using Microsoft.AspNetCore.Mvc;
using WhatsApp.API.DTOs.Auth;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var (user, token, expiresIn) = await _authService.LoginAsync(
                request.ClientId,
                request.Email,
                request.Password);

            var response = new AuthResponse
            {
                Token = token,
                ExpiresIn = expiresIn,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    TenantId = user.TenantId,
                    TenantName = user.Tenant?.Name ?? string.Empty,
                    ClientId = user.Tenant?.ClientId ?? string.Empty
                }
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return BadRequest(new { error = "Bad Request", message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Success message</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(
                request.ClientId,
                request.Email,
                request.Password,
                request.FullName,
                request.Role);

            return CreatedAtAction(
                nameof(Login),
                new
                {
                    message = "User registered successfully",
                    userId = user.Id,
                    email = user.Email
                });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { error = "Bad Request", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return BadRequest(new { error = "Bad Request", message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    /// <returns>User details</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        // This endpoint would require JWT authentication middleware
        // For now, returning NotImplemented
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "This endpoint requires authentication middleware to be fully configured"
        });
    }
}
