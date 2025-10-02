using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;
using WhatsApp.Core.Models;
using WhatsApp.Infrastructure.Data;
using WhatsApp.Infrastructure.Data.Repositories;
using WhatsApp.Infrastructure.Services;

namespace WhatsApp.Tests.Integration;

public class MessageIntegrationTests : IAsyncLifetime
{
    private SupabaseContext _context = null!;
    private ISessionRepository _sessionRepository = null!;
    private IMessageRepository _messageRepository = null!;
    private Mock<IWhatsAppProvider> _providerMock = null!;
    private MessageService _messageService = null!;
    private Tenant _testTenant = null!;
    private WhatsAppSession _testSession = null!;

    public async Task InitializeAsync()
    {
        // Setup in-memory or real database
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RulesEngineDatabase"] = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;Command Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;Pooling=true;MinPoolSize=2;MaxPoolSize=10;"
            })
            .Build();

        var connectionString = configuration.GetConnectionString("RulesEngineDatabase");

        var options = new DbContextOptionsBuilder<SupabaseContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new SupabaseContext(options);

        // Create repositories
        _sessionRepository = new SessionRepository(_context);
        _messageRepository = new MessageRepository(_context);

        // Mock provider
        _providerMock = new Mock<IWhatsAppProvider>();
        _providerMock
            .Setup(x => x.SendTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string to, string content, CancellationToken ct) => new MessageResult
            {
                MessageId = $"test-{Guid.NewGuid():N}",
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow
            });

        _providerMock
            .Setup(x => x.SendMediaAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<MessageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string to, byte[] media, MessageType type, string? caption, CancellationToken ct) => new MessageResult
            {
                MessageId = $"test-media-{Guid.NewGuid():N}",
                Status = MessageStatus.Sent,
                Provider = "baileys",
                Timestamp = DateTime.UtcNow
            });

        var loggerMock = new Mock<ILogger<MessageService>>();

        // Create service
        _messageService = new MessageService(
            _sessionRepository,
            _messageRepository,
            _providerMock.Object,
            loggerMock.Object);

        // Create test data
        _testTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = $"integration-test-{Guid.NewGuid()}",
            Name = "Integration Test Tenant",
            Settings = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(_testTenant);

        _testSession = new WhatsAppSession
        {
            Id = Guid.NewGuid(),
            TenantId = _testTenant.Id,
            PhoneNumber = "+5511888888888",
            ProviderType = ProviderType.Baileys,
            IsActive = true,
            SessionData = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.WhatsAppSessions.AddAsync(_testSession);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        var messages = await _context.Messages.Where(m => m.TenantId == _testTenant.Id).ToListAsync();
        _context.Messages.RemoveRange(messages);

        var sessions = await _context.WhatsAppSessions.Where(s => s.TenantId == _testTenant.Id).ToListAsync();
        _context.WhatsAppSessions.RemoveRange(sessions);

        var tenant = await _context.Tenants.FindAsync(_testTenant.Id);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
        }

        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Should_Send_Text_Message_And_Persist_To_Database()
    {
        // Arrange
        var to = "+5511999999999";
        var content = "Integration test message";

        // Act
        var result = await _messageService.SendTextAsync(_testTenant.Id, to, content);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.NotEmpty(result.MessageId);

        // Verify persistence
        var savedMessage = await _context.Messages
            .FirstOrDefaultAsync(m => m.MessageId == result.MessageId);

        Assert.NotNull(savedMessage);
        Assert.Equal(_testTenant.Id, savedMessage.TenantId);
        Assert.Equal(_testSession.Id, savedMessage.SessionId);
        Assert.Equal(MessageType.Text, savedMessage.MessageType);
        Assert.Equal(to, savedMessage.ToNumber);
        Assert.Equal(_testSession.PhoneNumber, savedMessage.FromNumber);
    }

    [Fact]
    public async Task Should_Send_Media_Message_And_Persist_To_Database()
    {
        // Arrange
        var to = "+5511999999999";
        var media = new byte[] { 1, 2, 3, 4, 5 };
        var mediaType = MessageType.Image;
        var caption = "Test image";

        // Act
        var result = await _messageService.SendMediaAsync(_testTenant.Id, to, media, mediaType, caption);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.NotEmpty(result.MessageId);

        // Verify persistence
        var savedMessage = await _context.Messages
            .FirstOrDefaultAsync(m => m.MessageId == result.MessageId);

        Assert.NotNull(savedMessage);
        Assert.Equal(MessageType.Image, savedMessage.MessageType);
    }

    [Fact]
    public async Task Should_Retrieve_Message_Status()
    {
        // Arrange - Send a message first
        var to = "+5511999999999";
        var content = "Test for status retrieval";
        var sendResult = await _messageService.SendTextAsync(_testTenant.Id, to, content);

        // Act
        var statusResult = await _messageService.GetMessageStatusAsync(_testTenant.Id, sendResult.MessageId);

        // Assert
        Assert.NotNull(statusResult);
        Assert.Equal(sendResult.MessageId, statusResult.MessageId);
        Assert.Equal(MessageStatus.Sent, statusResult.Status);
    }

    [Fact]
    public async Task Should_Return_Failed_When_No_Active_Session()
    {
        // Arrange - Deactivate session
        _testSession.IsActive = false;
        _context.WhatsAppSessions.Update(_testSession);
        await _context.SaveChangesAsync();

        var to = "+5511999999999";
        var content = "Test message";

        // Act
        var result = await _messageService.SendTextAsync(_testTenant.Id, to, content);

        // Assert
        Assert.Equal(MessageStatus.Failed, result.Status);
        Assert.Contains("No active WhatsApp session", result.Error);

        // Reactivate for cleanup
        _testSession.IsActive = true;
        _context.WhatsAppSessions.Update(_testSession);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_Not_Return_Message_From_Different_Tenant()
    {
        // Arrange - Create another tenant
        var otherTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = $"other-tenant-{Guid.NewGuid()}",
            Name = "Other Tenant",
            Settings = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(otherTenant);
        await _context.SaveChangesAsync();

        // Send message from test tenant
        var to = "+5511999999999";
        var content = "Tenant isolation test";
        var sendResult = await _messageService.SendTextAsync(_testTenant.Id, to, content);

        // Act - Try to retrieve from different tenant
        var statusResult = await _messageService.GetMessageStatusAsync(otherTenant.Id, sendResult.MessageId);

        // Assert
        Assert.Null(statusResult);

        // Cleanup
        _context.Tenants.Remove(otherTenant);
        await _context.SaveChangesAsync();
    }
}