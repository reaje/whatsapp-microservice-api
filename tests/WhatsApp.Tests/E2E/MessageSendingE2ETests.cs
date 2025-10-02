using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;
using WhatsApp.Core.Models;
using WhatsApp.Infrastructure.Data;
using WhatsApp.Infrastructure.Data.Repositories;
using WhatsApp.Infrastructure.Providers;
using WhatsApp.Infrastructure.Services;

namespace WhatsApp.Tests.E2E;

/// <summary>
/// End-to-End tests for real message sending with Baileys.
/// Requires:
/// 1. baileys-service running on http://localhost:3000
/// 2. Active WhatsApp session (already connected and scanned QR)
/// 3. Real tenant in database: a4876b9d-8ce5-4b67-ab69-c04073ce2f80
///
/// To run:
/// dotnet test --filter "FullyQualifiedName~MessageSendingE2ETests" --logger "console;verbosity=detailed"
///
/// IMPORTANT: Change TEST_RECIPIENT_NUMBER to a different number than the session phone!
/// </summary>
public class MessageSendingE2ETests : IAsyncLifetime
{
    private const string SESSION_PHONE_NUMBER = "+5571991776091";
    private const string TEST_RECIPIENT_NUMBER = "+5571887299930";
    private static readonly Guid REAL_TENANT_ID = Guid.Parse("b8f3c5d0-7f1e-4a2b-9c3d-1e8f4a6b2c7d");

    private SupabaseContext _context = null!;
    private ISessionRepository _sessionRepository = null!;
    private IMessageRepository _messageRepository = null!;
    private BaileysProvider _baileysProvider = null!;
    private MessageService _messageService = null!;
    private IConfiguration _configuration = null!;
    private ILogger<MessageSendingE2ETests> _testLogger = null!;

    public async Task InitializeAsync()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _testLogger = loggerFactory.CreateLogger<MessageSendingE2ETests>();

        // Configuration
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RulesEngineDatabase"] = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.yzhqgoofrxixndfcfucz;Password=8PrqjzQegAgFHnM4;Timeout=30;Command Timeout=30;SSL Mode=Require;Trust Server Certificate=true;Search Path=whatsapp_service;Pooling=true;MinPoolSize=2;MaxPoolSize=10;",
                ["BaileysService:Url"] = "http://localhost:3000"
            })
            .Build();

        // Database context
        var options = new DbContextOptionsBuilder<SupabaseContext>()
            .UseNpgsql(_configuration.GetConnectionString("RulesEngineDatabase"))
            .Options;
        _context = new SupabaseContext(options);

        // Repositories
        _sessionRepository = new SessionRepository(_context);
        _messageRepository = new MessageRepository(_context);

        // Baileys provider
        var baileysLogger = loggerFactory.CreateLogger<BaileysProvider>();
        var httpClientFactory = new TestHttpClientFactory();
        _baileysProvider = new BaileysProvider(baileysLogger, httpClientFactory, _configuration);

        // Message service
        var messageLogger = loggerFactory.CreateLogger<MessageService>();
        _messageService = new MessageService(
            _sessionRepository,
            _messageRepository,
            _baileysProvider,
            messageLogger);

        _testLogger.LogInformation("E2E Test initialized - Using real tenant: {TenantId}", REAL_TENANT_ID);
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Only delete test messages
        var messages = await _context.Messages
            .Where(m => m.TenantId == REAL_TENANT_ID && m.ToNumber == TEST_RECIPIENT_NUMBER)
            .ToListAsync();

        if (messages.Any())
        {
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            _testLogger.LogInformation("Cleaned up {Count} test messages", messages.Count);
        }

        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Should_Send_Real_Text_Message_End_To_End()
    {
        // Arrange
        _testLogger.LogInformation("=== Starting E2E Text Message Test ===");
        _testLogger.LogInformation("Tenant ID: {TenantId}", REAL_TENANT_ID);
        _testLogger.LogInformation("Recipient: {Recipient}", TEST_RECIPIENT_NUMBER);

        // Verify tenant exists
        var tenant = await _context.Tenants.FindAsync(REAL_TENANT_ID);
        Assert.NotNull(tenant);
        _testLogger.LogInformation("✅ Tenant found: {TenantName}", tenant.Name);

        // Verify active session exists
        var sessions = await _sessionRepository.GetActivesessionsByTenantAsync(REAL_TENANT_ID);
        var session = sessions.FirstOrDefault();
        Assert.NotNull(session);
        _testLogger.LogInformation("✅ Active session found: {PhoneNumber}", session.PhoneNumber);

        // Prepare message
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var content = $"🚀 E2E Test Message - {timestamp}";

        _testLogger.LogInformation("Sending message: {Content}", content);

        // Act - Send message through the service (full flow)
        var result = await _messageService.SendTextAsync(
            REAL_TENANT_ID,
            TEST_RECIPIENT_NUMBER,
            content);

        // Assert
        Assert.NotNull(result);
        _testLogger.LogInformation("Result Status: {Status}", result.Status);
        _testLogger.LogInformation("Result Message ID: {MessageId}", result.MessageId);
        _testLogger.LogInformation("Result Provider: {Provider}", result.Provider);
        _testLogger.LogInformation("Result Error: {Error}", result.Error ?? "None");

        if (result.Error != null)
        {
            _testLogger.LogError("⚠️ Message sending failed with error: {Error}", result.Error);
            _testLogger.LogWarning("This error is expected if you're trying to send to the same number as the session");
            _testLogger.LogWarning("Change TEST_RECIPIENT_NUMBER in the test to a DIFFERENT WhatsApp number!");
        }

        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.NotEmpty(result.MessageId);
        Assert.Equal("baileys", result.Provider);
        Assert.Null(result.Error);

        _testLogger.LogInformation("✅ Message sent successfully!");

        // Verify message was saved to database
        await Task.Delay(1000); // Wait for async save
        var savedMessage = await _messageRepository.GetByMessageIdAsync(result.MessageId);
        Assert.NotNull(savedMessage);
        Assert.Equal(REAL_TENANT_ID, savedMessage.TenantId);
        Assert.Equal(TEST_RECIPIENT_NUMBER, savedMessage.ToNumber);
        _testLogger.LogInformation("✅ Message saved to database");

        _testLogger.LogInformation("=== E2E Test Completed Successfully ===");
    }

    [Fact(Skip = "Run this after fixing TEST_RECIPIENT_NUMBER")]
    public async Task Should_Send_Multiple_Messages_End_To_End()
    {
        // Arrange
        _testLogger.LogInformation("=== Starting Multiple Messages E2E Test ===");

        var messages = new[]
        {
            $"📱 Message 1 at {DateTime.UtcNow:HH:mm:ss}",
            $"📱 Message 2 at {DateTime.UtcNow:HH:mm:ss}",
            $"📱 Message 3 at {DateTime.UtcNow:HH:mm:ss}"
        };

        var results = new List<MessageResult>();

        // Act
        foreach (var content in messages)
        {
            _testLogger.LogInformation("Sending: {Content}", content);
            var result = await _messageService.SendTextAsync(
                REAL_TENANT_ID,
                TEST_RECIPIENT_NUMBER,
                content);

            results.Add(result);
            await Task.Delay(1000); // Delay between messages
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal(MessageStatus.Sent, r.Status);
            Assert.NotEmpty(r.MessageId);
            Assert.Null(r.Error);
        });

        _testLogger.LogInformation("✅ All {Count} messages sent successfully!", results.Count);
        _testLogger.LogInformation("=== Multiple Messages Test Completed ===");
    }

    [Fact]
    public async Task Should_Handle_Session_Not_Found_Gracefully()
    {
        // Arrange
        var fakeTenantId = Guid.NewGuid();
        _testLogger.LogInformation("Testing with non-existent tenant: {TenantId}", fakeTenantId);

        // Act
        var result = await _messageService.SendTextAsync(
            fakeTenantId,
            TEST_RECIPIENT_NUMBER,
            "This should fail");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MessageStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Contains("No active WhatsApp session", result.Error);
        _testLogger.LogInformation("✅ Error handled correctly: {Error}", result.Error);
    }
}

/// <summary>
/// Simple HttpClientFactory for E2E testing
/// </summary>
internal class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient
        {
            BaseAddress = new Uri("http://localhost:3000"),
            Timeout = TimeSpan.FromSeconds(60) // Increased timeout for real operations
        };
    }
}
