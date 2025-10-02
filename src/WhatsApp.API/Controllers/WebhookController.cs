using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WhatsApp.API.DTOs;
using WhatsApp.API.Extensions;
using WhatsApp.Core.Entities;
using WhatsApp.Core.Enums;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IMessageRepository messageRepository,
        ISessionRepository sessionRepository,
        ILogger<WebhookController> logger)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint for receiving incoming WhatsApp messages
    /// </summary>
    [HttpPost("incoming-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IncomingMessage([FromBody] IncomingMessageWebhookDto webhook, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Received incoming message webhook for tenant {TenantId}, from {From}", tenantId, webhook.From);

            // Find session by phone number
            var session = await _sessionRepository.GetByTenantAndPhoneAsync(tenantId, webhook.To, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Session not found for incoming message. Tenant {TenantId}, To {To}", tenantId, webhook.To);
                return BadRequest(new { error = "Session not found for recipient phone number" });
            }

            // Save incoming message to database
            var content = new
            {
                text = webhook.TextContent,
                mediaUrl = webhook.MediaUrl,
                mediaMimeType = webhook.MediaMimeType
            };

            var message = new Message
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SessionId = session.Id,
                MessageId = webhook.MessageId,
                FromNumber = webhook.From,
                ToNumber = webhook.To,
                MessageType = webhook.Type,
                Content = JsonDocument.Parse(JsonSerializer.Serialize(content)),
                Status = MessageStatus.Received,
                AiProcessed = false,
                CreatedAt = webhook.Timestamp,
                UpdatedAt = webhook.Timestamp
            };

            await _messageRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation("Incoming message saved successfully: {MessageId}", webhook.MessageId);

            // TODO: Trigger AI agent processing if configured
            // await _aiAgentService.ProcessIncomingMessageAsync(tenantId, message, cancellationToken);

            return Ok(new { message = "Webhook received and processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming message webhook");
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    /// <summary>
    /// Webhook endpoint for receiving message status updates
    /// </summary>
    [HttpPost("status-update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StatusUpdate([FromBody] MessageStatusUpdateWebhookDto webhook, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Received status update webhook for tenant {TenantId}, message {MessageId}, status {Status}",
                tenantId, webhook.MessageId, webhook.Status);

            // Find message by message ID
            var message = await _messageRepository.GetByMessageIdAsync(webhook.MessageId, cancellationToken);

            if (message == null)
            {
                _logger.LogWarning("Message not found for status update. MessageId {MessageId}", webhook.MessageId);
                return NotFound(new { error = "Message not found" });
            }

            // Verify tenant ownership
            if (message.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant mismatch for status update. Expected {ExpectedTenantId}, Got {ActualTenantId}",
                    message.TenantId, tenantId);
                return Unauthorized(new { error = "Unauthorized to update this message" });
            }

            // Update message status
            message.Status = webhook.Status;
            message.UpdatedAt = webhook.Timestamp;

            // Store error if present
            if (!string.IsNullOrEmpty(webhook.Error))
            {
                var content = message.Content?.RootElement.Clone();
                var contentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(content?.GetRawText() ?? "{}");
                if (contentDict != null)
                {
                    contentDict["error"] = webhook.Error;
                    message.Content = JsonDocument.Parse(JsonSerializer.Serialize(contentDict));
                }
            }

            await _messageRepository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation("Message status updated successfully: {MessageId} -> {Status}", webhook.MessageId, webhook.Status);

            return Ok(new { message = "Status update processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing status update webhook");
            return StatusCode(500, new { error = "Internal server error processing webhook" });
        }
    }

    /// <summary>
    /// Webhook verification endpoint (for Meta WhatsApp Business API)
    /// </summary>
    [HttpGet("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string mode,
                                 [FromQuery(Name = "hub.verify_token")] string verifyToken,
                                 [FromQuery(Name = "hub.challenge")] string challenge)
    {
        _logger.LogInformation("Received webhook verification request. Mode: {Mode}, Token: {Token}", mode, verifyToken);

        // TODO: Get verify token from configuration
        const string expectedToken = "your-verify-token";

        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(challenge);
        }

        _logger.LogWarning("Webhook verification failed. Invalid token or mode");
        return StatusCode(403, new { error = "Verification failed" });
    }
}