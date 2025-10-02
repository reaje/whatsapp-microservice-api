using System.Security.Claims;
using WhatsApp.Core.Entities;

namespace WhatsApp.API.Extensions;

public static class HttpContextExtensions
{
    public static Tenant? GetTenant(this HttpContext context)
    {
        if (context.Items.TryGetValue("Tenant", out var tenant))
        {
            return tenant as Tenant;
        }
        return null;
    }

    public static Guid GetTenantId(this HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var tenantId) && tenantId is Guid guid)
        {
            return guid;
        }
        throw new InvalidOperationException("Tenant ID not found in context. Ensure TenantMiddleware is configured.");
    }

    public static string? GetClientId(this HttpContext context)
    {
        if (context.Items.TryGetValue("ClientId", out var clientId))
        {
            return clientId as string;
        }
        return null;
    }

    public static string? GetUserRole(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.Role);
        return claim?.Value;
    }

    public static Guid? GetUserId(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}