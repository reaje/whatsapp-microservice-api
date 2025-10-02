using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Skip tenant validation for certain paths
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/health") ||
            path.StartsWith("/openapi") ||
            path.StartsWith("/scalar") ||
            path.Contains("/_vs/") ||
            path.Contains("/_framework/") ||
            path.StartsWith("/api/v1/auth") || // Skip auth endpoints
            (path.StartsWith("/api/v1/tenant") && context.Request.Method == "POST") ||
            (path.StartsWith("/api/v1/tenant") && context.Request.Method == "GET" && !path.Contains("/settings")))
        {
            await _next(context);
            return;
        }

        // Get client ID from header
        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogWarning("Request without X-Client-Id header to path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Bad Request",
                message = "X-Client-Id header is required"
            });
            return;
        }

        // Validate and load tenant
        var tenant = await tenantService.GetByClientIdAsync(clientId);
        if (tenant == null)
        {
            _logger.LogWarning("Invalid tenant client ID: {ClientId} from IP: {IP}",
                clientId, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid tenant credentials"
            });
            return;
        }

        // Store tenant in HttpContext.Items for downstream use
        context.Items["Tenant"] = tenant;
        context.Items["TenantId"] = tenant.Id;
        context.Items["ClientId"] = tenant.ClientId;

        _logger.LogDebug("Tenant validated: {TenantId} - {ClientId}", tenant.Id, tenant.ClientId);

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}