using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WhatsApp.API.Middleware;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.Tests.Unit;

public class TenantMiddlewareTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly TenantMiddleware _middleware;
    private readonly RequestDelegate _next;

    public TenantMiddlewareTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
        _next = (HttpContext context) => Task.CompletedTask;
        _middleware = new TenantMiddleware(_next, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Skip_Validation_For_Health_Endpoint()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        // Act
        await _middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        _tenantServiceMock.Verify(x => x.GetByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Skip_Validation_For_Documentation_Endpoints()
    {
        // Arrange
        var scalarContext = new DefaultHttpContext();
        scalarContext.Request.Path = "/scalar/v1";

        var openApiContext = new DefaultHttpContext();
        openApiContext.Request.Path = "/openapi/v1.json";

        // Act
        await _middleware.InvokeAsync(scalarContext, _tenantServiceMock.Object);
        await _middleware.InvokeAsync(openApiContext, _tenantServiceMock.Object);

        // Assert
        Assert.Equal(200, scalarContext.Response.StatusCode);
        Assert.Equal(200, openApiContext.Response.StatusCode);
        _tenantServiceMock.Verify(x => x.GetByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Return_400_When_ClientId_Header_Missing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/tenant/settings";
        context.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        _tenantServiceMock.Verify(x => x.GetByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Return_401_When_Tenant_Not_Found()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/tenant/settings";
        context.Request.Headers["X-Client-Id"] = "invalid-client-id";
        context.Response.Body = new MemoryStream();

        _tenantServiceMock
            .Setup(x => x.GetByClientIdAsync("invalid-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        await _middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        _tenantServiceMock.Verify(x => x.GetByClientIdAsync("invalid-client-id", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Add_Tenant_To_Context_When_Valid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/tenant/settings";
        context.Request.Headers["X-Client-Id"] = "valid-client-id";

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = "valid-client-id",
            Name = "Test Tenant",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tenantServiceMock
            .Setup(x => x.GetByClientIdAsync("valid-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _middleware.InvokeAsync(context, _tenantServiceMock.Object);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
        Assert.True(context.Items.ContainsKey("Tenant"));
        Assert.True(context.Items.ContainsKey("TenantId"));
        Assert.True(context.Items.ContainsKey("ClientId"));
        Assert.Equal(tenant, context.Items["Tenant"]);
        Assert.Equal(tenant.Id, context.Items["TenantId"]);
        Assert.Equal(tenant.ClientId, context.Items["ClientId"]);
        _tenantServiceMock.Verify(x => x.GetByClientIdAsync("valid-client-id", It.IsAny<CancellationToken>()), Times.Once);
    }
}