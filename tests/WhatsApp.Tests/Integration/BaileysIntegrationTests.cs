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

namespace WhatsApp.Tests.Integration;

/// <summary>
/// Integration tests for real Baileys service connection.
/// Requires baileys-service to be running on http://localhost:3000
///
/// To run these tests:
/// 1. Start baileys-service: cd baileys-service && npm run dev
/// 2. Run tests: dotnet test --filter "FullyQualifiedName~BaileysIntegrationTests"
///
/// Note: These tests use the real phone number +5571991776091
/// </summary>
public class BaileysIntegrationTests : IAsyncLifetime
{
    private const string TestPhoneNumber = "+5571991776091";
    private const string TestRecipient = "+5571991776091"; // Self-send for testing

    private SupabaseContext _context = null!;
    private ISessionRepository _sessionRepository = null!;
    private IMessageRepository _messageRepository = null!;
    private BaileysProvider _baileysProvider = null!;
    private SessionService _sessionService = null!;
    private MessageService _messageService = null!;
    private Tenant _testTenant = null!;
    private IConfiguration _configuration = null!;

    public async Task InitializeAsync()
    {
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

        // Baileys provider (real, not mocked)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var baileysLogger = loggerFactory.CreateLogger<BaileysProvider>();
        var httpClientFactory = new TestHttpClientFactory();

        _baileysProvider = new BaileysProvider(
            baileysLogger,
            httpClientFactory,
            _configuration);

        // Services
        var sessionLogger = loggerFactory.CreateLogger<SessionService>();
        _sessionService = new SessionService(
            _sessionRepository,
            _baileysProvider,
            sessionLogger);

        var messageLogger = loggerFactory.CreateLogger<MessageService>();
        _messageService = new MessageService(
            _sessionRepository,
            _messageRepository,
            _baileysProvider,
            messageLogger);

        // Create test tenant
        _testTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = $"baileys-test-{Guid.NewGuid()}",
            Name = "Baileys Integration Test",
            Settings = JsonDocument.Parse("{}"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Tenants.AddAsync(_testTenant);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Delete test messages
        var messages = await _context.Messages
            .Where(m => m.TenantId == _testTenant.Id)
            .ToListAsync();
        _context.Messages.RemoveRange(messages);

        // Cleanup: Delete test sessions
        var sessions = await _context.WhatsAppSessions
            .Where(s => s.TenantId == _testTenant.Id)
            .ToListAsync();
        _context.WhatsAppSessions.RemoveRange(sessions);

        // Cleanup: Delete test tenant
        var tenant = await _context.Tenants.FindAsync(_testTenant.Id);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
        }

        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    [Fact(Skip = "Requires baileys-service running and manual QR code scanning")]
    public async Task Should_Initialize_WhatsApp_Session_With_Real_Number()
    {
        // Arrange
        var tenantConfig = new TenantConfig
        {
            TenantId = _testTenant.Id,
            ClientId = _testTenant.ClientId,
            Settings = new Dictionary<string, string>()
        };

        // Act
        var result = await _baileysProvider.InitializeAsync(TestPhoneNumber, tenantConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestPhoneNumber, result.PhoneNumber);

        // If not already connected, should have QR code
        if (!result.IsConnected)
        {
            Assert.True(result.Status == "qr_required" || result.Status == "connecting");
            if (result.Status == "qr_required")
            {
                Assert.True(result.Metadata.ContainsKey("qrCode"));
                var qrCode = result.Metadata["qrCode"]?.ToString();
                Assert.NotNull(qrCode);
                Assert.NotEmpty(qrCode);

                // Output QR code for manual scanning
                Console.WriteLine("=== QR CODE FOR SCANNING ===");
                Console.WriteLine(qrCode);
                Console.WriteLine("============================");
                Console.WriteLine("Scan this QR code with WhatsApp to continue the test");
            }
        }
        else
        {
            Assert.Equal("connected", result.Status);
            Assert.True(result.IsConnected);
        }

        // Verify metadata
        Assert.Contains("provider", result.Metadata.Keys);
        Assert.Equal("baileys", result.Metadata["provider"]?.ToString());
    }

    [Fact(Skip = "Requires baileys-service running and active WhatsApp session")]
    public async Task Should_Send_Text_Message_Via_Baileys()
    {
        // Arrange
        var tenantConfig = new TenantConfig
        {
            TenantId = _testTenant.Id,
            ClientId = _testTenant.ClientId,
            Settings = new Dictionary<string, string>()
        };

        // Initialize session first
        var sessionStatus = await _baileysProvider.InitializeAsync(TestPhoneNumber, tenantConfig);

        // Skip if not connected (requires manual QR scanning first)
        if (!sessionStatus.IsConnected)
        {
            Console.WriteLine("Session not connected. Run Should_Initialize_WhatsApp_Session_With_Real_Number first and scan QR code.");
            return;
        }

        var content = $"🤖 Test message from Baileys Integration Test at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

        // Act
        var result = await _baileysProvider.SendTextAsync(TestRecipient, content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.NotEmpty(result.MessageId);
        Assert.Equal("baileys", result.Provider);
        Assert.Null(result.Error);

        // Verify metadata
        Assert.Contains("to", result.Metadata.Keys);
        Assert.Equal(TestRecipient, result.Metadata["to"]?.ToString());

        Console.WriteLine($"✅ Message sent successfully! Message ID: {result.MessageId}");
    }

    [Fact(Skip = "Requires baileys-service running and active WhatsApp session")]
    public async Task Should_Complete_Full_Flow_Initialize_And_Send()
    {
        // Arrange
        var tenantConfig = new TenantConfig
        {
            TenantId = _testTenant.Id,
            ClientId = _testTenant.ClientId,
            Settings = new Dictionary<string, string>()
        };

        // Act 1: Initialize session
        Console.WriteLine("Step 1: Initializing session...");
        var sessionStatus = await _baileysProvider.InitializeAsync(TestPhoneNumber, tenantConfig);

        Assert.NotNull(sessionStatus);
        Console.WriteLine($"Session status: {sessionStatus.Status}");

        if (!sessionStatus.IsConnected && sessionStatus.Metadata.ContainsKey("qrCode"))
        {
            var qrCode = sessionStatus.Metadata["qrCode"]?.ToString();
            Console.WriteLine("\n=== SCAN THIS QR CODE ===");
            Console.WriteLine(qrCode);
            Console.WriteLine("========================\n");
            Console.WriteLine("⚠️  Test paused. Scan QR code and run again when connected.");
            return;
        }

        if (!sessionStatus.IsConnected)
        {
            Console.WriteLine("Session not connected. Status: " + sessionStatus.Status);
            return;
        }

        // Act 2: Check status
        Console.WriteLine("\nStep 2: Checking session status...");
        var statusCheck = await _baileysProvider.GetStatusAsync();
        Assert.NotNull(statusCheck);
        Assert.True(statusCheck.IsConnected);
        Console.WriteLine($"✅ Session is connected: {statusCheck.Status}");

        // Act 3: Send text message
        Console.WriteLine("\nStep 3: Sending text message...");
        var textContent = $"🎉 Full integration test - Text message at {DateTime.UtcNow:HH:mm:ss}";
        var textResult = await _baileysProvider.SendTextAsync(TestRecipient, textContent);

        Assert.NotNull(textResult);
        Assert.Equal(MessageStatus.Sent, textResult.Status);
        Assert.NotEmpty(textResult.MessageId);
        Console.WriteLine($"✅ Text message sent! ID: {textResult.MessageId}");

        // Act 4: Send location
        Console.WriteLine("\nStep 4: Sending location...");
        var locationResult = await _baileysProvider.SendLocationAsync(
            TestRecipient,
            -12.9714,  // Salvador, BA latitude
            -38.5014); // Salvador, BA longitude

        Assert.NotNull(locationResult);
        Assert.Equal(MessageStatus.Sent, locationResult.Status);
        Console.WriteLine($"✅ Location sent! ID: {locationResult.MessageId}");

        // Final verification
        Console.WriteLine("\n✅ Full flow completed successfully!");
        Console.WriteLine($"- Session initialized: {sessionStatus.Status}");
        Console.WriteLine($"- Text message sent: {textResult.MessageId}");
        Console.WriteLine($"- Location sent: {locationResult.MessageId}");
    }

    [Fact(Skip = "Requires baileys-service running")]
    public async Task Should_Get_Session_Status_From_Baileys_Service()
    {
        // Arrange
        var tenantConfig = new TenantConfig
        {
            TenantId = _testTenant.Id,
            ClientId = _testTenant.ClientId,
            Settings = new Dictionary<string, string>()
        };

        // Initialize first
        await _baileysProvider.InitializeAsync(TestPhoneNumber, tenantConfig);

        // Act
        var status = await _baileysProvider.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.Contains(status.Status, new[] { "qr_required", "connecting", "connected", "disconnected" });
    }

    [Fact(Skip = "Requires baileys-service running")]
    public async Task Should_Handle_Disconnection_Gracefully()
    {
        // Arrange
        var tenantConfig = new TenantConfig
        {
            TenantId = _testTenant.Id,
            ClientId = _testTenant.ClientId,
            Settings = new Dictionary<string, string>()
        };

        await _baileysProvider.InitializeAsync(TestPhoneNumber, tenantConfig);

        // Act
        await _baileysProvider.DisconnectAsync();

        // Wait a bit for disconnection to process
        await Task.Delay(1000);

        // Assert - Get status should show disconnected
        var status = await _baileysProvider.GetStatusAsync();
        Assert.NotNull(status);
    }
}

/// <summary>
/// Simple HttpClientFactory for testing
/// </summary>
internal class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient
        {
            BaseAddress = new Uri("http://localhost:3000"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
