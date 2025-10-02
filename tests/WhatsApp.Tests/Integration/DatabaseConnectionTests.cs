using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WhatsApp.Core.Entities;
using WhatsApp.Infrastructure.Data;
using WhatsApp.Infrastructure.Data.Repositories;

namespace WhatsApp.Tests.Integration;

public class DatabaseConnectionTests : IDisposable
{
    private readonly SupabaseContext _context;
    private readonly TenantRepository _tenantRepository;

    public DatabaseConnectionTests()
    {
        // Build configuration - try multiple paths
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddJsonFile(Path.Combine(basePath, "appsettings.Test.json"), optional: true)
            .AddJsonFile(Path.Combine(basePath, "../../../appsettings.Test.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("RulesEngineDatabase");

        // Fallback to hardcoded connection string if not found in config
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;Command Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;Pooling=true;MinPoolSize=2;MaxPoolSize=10;";
        }

        // Create DbContext
        var options = new DbContextOptionsBuilder<SupabaseContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new SupabaseContext(options);
        _tenantRepository = new TenantRepository(_context);
    }

    [Fact]
    public async Task Should_Connect_To_Database()
    {
        // Act
        var canConnect = await _context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect, "Should be able to connect to database");
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Tenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            ClientId = $"test-client-{Guid.NewGuid()}",
            Name = "Test Tenant",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            // Act - Create
            var createdTenant = await _tenantRepository.AddAsync(tenant);

            // Assert - Created
            Assert.NotEqual(Guid.Empty, createdTenant.Id);
            Assert.Equal(tenant.ClientId, createdTenant.ClientId);

            // Act - Retrieve
            var retrievedTenant = await _tenantRepository.GetByClientIdAsync(tenant.ClientId);

            // Assert - Retrieved
            Assert.NotNull(retrievedTenant);
            Assert.Equal(createdTenant.Id, retrievedTenant.Id);
            Assert.Equal(createdTenant.ClientId, retrievedTenant.ClientId);
            Assert.Equal(createdTenant.Name, retrievedTenant.Name);
        }
        finally
        {
            // Cleanup
            if (tenant.Id != Guid.Empty)
            {
                await _tenantRepository.DeleteAsync(tenant);
            }
        }
    }

    [Fact]
    public async Task Should_Update_Tenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            ClientId = $"test-client-{Guid.NewGuid()}",
            Name = "Original Name",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            // Act - Create
            await _tenantRepository.AddAsync(tenant);

            // Update
            tenant.Name = "Updated Name";
            tenant.UpdatedAt = DateTime.UtcNow;
            await _tenantRepository.UpdateAsync(tenant);

            // Retrieve
            var updatedTenant = await _tenantRepository.GetByIdAsync(tenant.Id);

            // Assert
            Assert.NotNull(updatedTenant);
            Assert.Equal("Updated Name", updatedTenant.Name);
        }
        finally
        {
            // Cleanup
            if (tenant.Id != Guid.Empty)
            {
                await _tenantRepository.DeleteAsync(tenant);
            }
        }
    }

    [Fact]
    public async Task Should_Delete_Tenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            ClientId = $"test-client-{Guid.NewGuid()}",
            Name = "To Be Deleted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Create
        await _tenantRepository.AddAsync(tenant);
        var tenantId = tenant.Id;

        // Delete
        await _tenantRepository.DeleteAsync(tenant);

        // Try to retrieve
        var deletedTenant = await _tenantRepository.GetByIdAsync(tenantId);

        // Assert
        Assert.Null(deletedTenant);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}