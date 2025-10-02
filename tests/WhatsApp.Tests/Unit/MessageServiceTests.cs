using Microsoft.Extensions.Logging;
using Moq;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;
using WhatsApp.Core.Models;
using WhatsApp.Infrastructure.Services;

namespace WhatsApp.Tests.Unit;

public class MessageServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<IWhatsAppProvider> _whatsAppProviderMock;
    private readonly Mock<ILogger<MessageService>> _loggerMock;
    private readonly MessageService _messageService;

    public MessageServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _whatsAppProviderMock = new Mock<IWhatsAppProvider>();
        _loggerMock = new Mock<ILogger<MessageService>>();

        _messageService = new MessageService(
            _sessionRepositoryMock.Object,
            _messageRepositoryMock.Object,
            _whatsAppProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SendTextAsync_Should_Return_Failed_When_No_Active_Session()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var to = "+5511999999999";
        var content = "Test message";

        _sessionRepositoryMock
            .Setup(x => x.GetActivesessionsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppSession>());

        // Act
        var result = await _messageService.SendTextAsync(tenantId, to, content);

        // Assert
        Assert.Equal(MessageStatus.Failed, result.Status);
        Assert.Contains("No active WhatsApp session", result.Error);
        _whatsAppProviderMock.Verify(x => x.SendTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendTextAsync_Should_Send_Message_And_Save_To_Database()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var to = "+5511999999999";
        var content = "Test message";
        var phoneNumber = "+5511888888888";

        var session = new WhatsAppSession
        {
            Id = sessionId,
            TenantId = tenantId,
            PhoneNumber = phoneNumber,
            IsActive = true
        };

        var providerResult = new MessageResult
        {
            MessageId = "test-message-id",
            Status = MessageStatus.Sent,
            Provider = "baileys",
            Timestamp = DateTime.UtcNow
        };

        _sessionRepositoryMock
            .Setup(x => x.GetActivesessionsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppSession> { session });

        _whatsAppProviderMock
            .Setup(x => x.SendTextAsync(to, content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResult);

        _messageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message m, CancellationToken ct) => m);

        // Act
        var result = await _messageService.SendTextAsync(tenantId, to, content);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.Equal("test-message-id", result.MessageId);
        Assert.Equal("baileys", result.Provider);

        _whatsAppProviderMock.Verify(x => x.SendTextAsync(to, content, It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m =>
            m.TenantId == tenantId &&
            m.SessionId == sessionId &&
            m.MessageId == "test-message-id" &&
            m.FromNumber == phoneNumber &&
            m.ToNumber == to &&
            m.MessageType == MessageType.Text &&
            m.Status == MessageStatus.Sent
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMediaAsync_Should_Send_Message_And_Save_To_Database()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var to = "+5511999999999";
        var media = new byte[] { 1, 2, 3, 4, 5 };
        var mediaType = MessageType.Image;
        var caption = "Test caption";

        var session = new WhatsAppSession
        {
            Id = sessionId,
            TenantId = tenantId,
            PhoneNumber = "+5511888888888",
            IsActive = true
        };

        var providerResult = new MessageResult
        {
            MessageId = "test-media-id",
            Status = MessageStatus.Sent,
            Provider = "baileys",
            Timestamp = DateTime.UtcNow
        };

        _sessionRepositoryMock
            .Setup(x => x.GetActivesessionsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppSession> { session });

        _whatsAppProviderMock
            .Setup(x => x.SendMediaAsync(to, media, mediaType, caption, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResult);

        _messageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message m, CancellationToken ct) => m);

        // Act
        var result = await _messageService.SendMediaAsync(tenantId, to, media, mediaType, caption);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.Equal("test-media-id", result.MessageId);

        _whatsAppProviderMock.Verify(x => x.SendMediaAsync(to, media, mediaType, caption, It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m =>
            m.TenantId == tenantId &&
            m.MessageType == MessageType.Image &&
            m.Status == MessageStatus.Sent
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendLocationAsync_Should_Send_Message_And_Save_To_Database()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var to = "+5511999999999";
        var latitude = -23.5505;
        var longitude = -46.6333;

        var session = new WhatsAppSession
        {
            Id = sessionId,
            TenantId = tenantId,
            PhoneNumber = "+5511888888888",
            IsActive = true
        };

        var providerResult = new MessageResult
        {
            MessageId = "test-location-id",
            Status = MessageStatus.Sent,
            Provider = "baileys",
            Timestamp = DateTime.UtcNow
        };

        _sessionRepositoryMock
            .Setup(x => x.GetActivesessionsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppSession> { session });

        _whatsAppProviderMock
            .Setup(x => x.SendLocationAsync(to, latitude, longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResult);

        _messageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message m, CancellationToken ct) => m);

        // Act
        var result = await _messageService.SendLocationAsync(tenantId, to, latitude, longitude);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.Equal("test-location-id", result.MessageId);

        _whatsAppProviderMock.Verify(x => x.SendLocationAsync(to, latitude, longitude, It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m =>
            m.MessageType == MessageType.Location
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAudioAsync_Should_Send_Message_And_Save_To_Database()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var to = "+5511999999999";
        var audio = new byte[] { 1, 2, 3, 4, 5 };

        var session = new WhatsAppSession
        {
            Id = sessionId,
            TenantId = tenantId,
            PhoneNumber = "+5511888888888",
            IsActive = true
        };

        var providerResult = new MessageResult
        {
            MessageId = "test-audio-id",
            Status = MessageStatus.Sent,
            Provider = "baileys",
            Timestamp = DateTime.UtcNow
        };

        _sessionRepositoryMock
            .Setup(x => x.GetActivesessionsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WhatsAppSession> { session });

        _whatsAppProviderMock
            .Setup(x => x.SendAudioAsync(to, audio, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerResult);

        _messageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message m, CancellationToken ct) => m);

        // Act
        var result = await _messageService.SendAudioAsync(tenantId, to, audio);

        // Assert
        Assert.Equal(MessageStatus.Sent, result.Status);
        Assert.Equal("test-audio-id", result.MessageId);

        _whatsAppProviderMock.Verify(x => x.SendAudioAsync(to, audio, It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m =>
            m.MessageType == MessageType.Audio
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessageStatusAsync_Should_Return_Null_When_Message_Not_Found()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var messageId = "non-existent-id";

        _messageRepositoryMock
            .Setup(x => x.GetByMessageIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _messageService.GetMessageStatusAsync(tenantId, messageId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMessageStatusAsync_Should_Return_Null_When_Tenant_Mismatch()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var messageId = "test-message-id";

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            MessageId = messageId,
            Status = MessageStatus.Sent
        };

        _messageRepositoryMock
            .Setup(x => x.GetByMessageIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _messageService.GetMessageStatusAsync(tenantId, messageId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMessageStatusAsync_Should_Return_Message_Status()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var messageId = "test-message-id";

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MessageId = messageId,
            Status = MessageStatus.Delivered,
            UpdatedAt = DateTime.UtcNow
        };

        _messageRepositoryMock
            .Setup(x => x.GetByMessageIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _messageService.GetMessageStatusAsync(tenantId, messageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(messageId, result.MessageId);
        Assert.Equal(MessageStatus.Delivered, result.Status);
    }
}