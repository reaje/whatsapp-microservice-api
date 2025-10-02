using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Infrastructure.Data;
using WhatsApp.Infrastructure.Data.Repositories;

namespace WhatsApp.Tests.Integration;

public class TenantIsolationTests : IDisposable
{
    private readonly SupabaseContext _context;
    private readonly TenantRepository _tenantRepository;
    private readonly SessionRepository _sessionRepository;
    private readonly MessageRepository _messageRepository;

    public TenantIsolationTests()
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("RulesEngineDatabase");
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;Command Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;Pooling=true;MinPoolSize=2;MaxPoolSize=10;";
        }

        var options = new DbContextOptionsBuilder<SupabaseContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new SupabaseContext(options);
        _tenantRepository = new TenantRepository(_context);
        _sessionRepository = new SessionRepository(_context);
        _messageRepository = new MessageRepository(_context);
    }

    [Fact]
    public async Task Should_Isolate_Sessions_Between_Tenants()
    {
        // Arrange - Create two tenants
        var tenant1 = new Tenant
        {
            ClientId = $"tenant1-{Guid.NewGuid()}",
            Name = "Tenant 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            ClientId = $"tenant2-{Guid.NewGuid()}",
            Name = "Tenant 2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _tenantRepository.AddAsync(tenant1);
            await _tenantRepository.AddAsync(tenant2);

            // Create sessions for each tenant
            var session1 = new WhatsAppSession
            {
                TenantId = tenant1.Id,
                PhoneNumber = "5511999999991",
                ProviderType = ProviderType.Baileys,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var session2 = new WhatsAppSession
            {
                TenantId = tenant2.Id,
                PhoneNumber = "5511999999992",
                ProviderType = ProviderType.Baileys,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sessionRepository.AddAsync(session1);
            await _sessionRepository.AddAsync(session2);

            // Act - Get sessions for tenant1
            var tenant1Sessions = await _sessionRepository.GetActivesessionsByTenantAsync(tenant1.Id);

            // Assert
            Assert.Single(tenant1Sessions);
            Assert.Equal(session1.PhoneNumber, tenant1Sessions.First().PhoneNumber);
            Assert.DoesNotContain(tenant1Sessions, s => s.PhoneNumber == session2.PhoneNumber);

            // Cleanup
            await _sessionRepository.DeleteAsync(session1);
            await _sessionRepository.DeleteAsync(session2);
        }
        finally
        {
            if (tenant1.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant1);
            if (tenant2.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant2);
        }
    }

    [Fact]
    public async Task Should_Isolate_Messages_Between_Tenants()
    {
        // Arrange - Create two tenants with sessions
        var tenant1 = new Tenant
        {
            ClientId = $"tenant1-msg-{Guid.NewGuid()}",
            Name = "Tenant 1 Messages",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            ClientId = $"tenant2-msg-{Guid.NewGuid()}",
            Name = "Tenant 2 Messages",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _tenantRepository.AddAsync(tenant1);
            await _tenantRepository.AddAsync(tenant2);

            var session1 = new WhatsAppSession
            {
                TenantId = tenant1.Id,
                PhoneNumber = "5511999999993",
                ProviderType = ProviderType.Baileys,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var session2 = new WhatsAppSession
            {
                TenantId = tenant2.Id,
                PhoneNumber = "5511999999994",
                ProviderType = ProviderType.Baileys,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sessionRepository.AddAsync(session1);
            await _sessionRepository.AddAsync(session2);

            // Create messages for each tenant
            var message1 = new Message
            {
                TenantId = tenant1.Id,
                SessionId = session1.Id,
                MessageId = $"msg-{Guid.NewGuid()}",
                FromNumber = "5511999999993",
                ToNumber = "5511888888881",
                MessageType = MessageType.Text,
                Status = MessageStatus.Sent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var message2 = new Message
            {
                TenantId = tenant2.Id,
                SessionId = session2.Id,
                MessageId = $"msg-{Guid.NewGuid()}",
                FromNumber = "5511999999994",
                ToNumber = "5511888888882",
                MessageType = MessageType.Text,
                Status = MessageStatus.Sent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message1);
            await _messageRepository.AddAsync(message2);

            // Act - Get messages for tenant1
            var tenant1Messages = await _messageRepository.GetByTenantAsync(tenant1.Id);

            // Assert
            Assert.Single(tenant1Messages);
            Assert.Equal(message1.MessageId, tenant1Messages.First().MessageId);
            Assert.DoesNotContain(tenant1Messages, m => m.MessageId == message2.MessageId);

            // Cleanup
            await _messageRepository.DeleteAsync(message1);
            await _messageRepository.DeleteAsync(message2);
            await _sessionRepository.DeleteAsync(session1);
            await _sessionRepository.DeleteAsync(session2);
        }
        finally
        {
            if (tenant1.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant1);
            if (tenant2.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant2);
        }
    }

    [Fact]
    public async Task Should_Not_Allow_Cross_Tenant_Data_Access()
    {
        // Arrange
        var tenant1 = new Tenant
        {
            ClientId = $"tenant1-cross-{Guid.NewGuid()}",
            Name = "Tenant 1 Cross",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            ClientId = $"tenant2-cross-{Guid.NewGuid()}",
            Name = "Tenant 2 Cross",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _tenantRepository.AddAsync(tenant1);
            await _tenantRepository.AddAsync(tenant2);

            var session1 = new WhatsAppSession
            {
                TenantId = tenant1.Id,
                PhoneNumber = "5511999999995",
                ProviderType = ProviderType.Baileys,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _sessionRepository.AddAsync(session1);

            // Act - Try to get tenant1's session using tenant2's ID
            var tenant2Sessions = await _sessionRepository.GetActivesessionsByTenantAsync(tenant2.Id);

            // Assert - Should not return tenant1's session
            Assert.Empty(tenant2Sessions);
            Assert.DoesNotContain(tenant2Sessions, s => s.Id == session1.Id);

            // Cleanup
            await _sessionRepository.DeleteAsync(session1);
        }
        finally
        {
            if (tenant1.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant1);
            if (tenant2.Id != Guid.Empty) await _tenantRepository.DeleteAsync(tenant2);
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}