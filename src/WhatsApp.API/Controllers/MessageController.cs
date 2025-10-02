using Microsoft.AspNetCore.Mvc;
using WhatsApp.API.DTOs;
using WhatsApp.API.Extensions;
using WhatsApp.Core.Interfaces;

namespace WhatsApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMessageService messageService, ILogger<MessageController> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Send a text message
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendText([FromBody] SendTextMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Sending text message for tenant {TenantId} to {To}", tenantId, request.To);

            var result = await _messageService.SendTextAsync(tenantId, request.To, request.Content, cancellationToken);

            var response = new MessageResponseDto
            {
                MessageId = result.MessageId,
                Status = result.Status,
                Provider = result.Provider,
                Timestamp = result.Timestamp,
                Error = result.Error,
                Metadata = result.Metadata
            };

            if (result.Status == Core.Enums.MessageStatus.Failed)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text message");
            return StatusCode(500, new { error = "Internal server error sending message" });
        }
    }

    /// <summary>
    /// Send a media message (image, video, document)
    /// </summary>
    [HttpPost("media")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendMedia([FromBody] SendMediaMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Sending media message for tenant {TenantId} to {To}, type {MediaType}",
                tenantId, request.To, request.MediaType);

            var mediaBytes = Convert.FromBase64String(request.MediaBase64);
            var result = await _messageService.SendMediaAsync(
                tenantId,
                request.To,
                mediaBytes,
                request.MediaType,
                request.Caption,
                cancellationToken);

            var response = new MessageResponseDto
            {
                MessageId = result.MessageId,
                Status = result.Status,
                Provider = result.Provider,
                Timestamp = result.Timestamp,
                Error = result.Error,
                Metadata = result.Metadata
            };

            if (result.Status == Core.Enums.MessageStatus.Failed)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid base64 media data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending media message");
            return StatusCode(500, new { error = "Internal server error sending message" });
        }
    }

    /// <summary>
    /// Send a location message
    /// </summary>
    [HttpPost("location")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendLocation([FromBody] SendLocationMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Sending location message for tenant {TenantId} to {To}", tenantId, request.To);

            var result = await _messageService.SendLocationAsync(
                tenantId,
                request.To,
                request.Latitude,
                request.Longitude,
                cancellationToken);

            var response = new MessageResponseDto
            {
                MessageId = result.MessageId,
                Status = result.Status,
                Provider = result.Provider,
                Timestamp = result.Timestamp,
                Error = result.Error,
                Metadata = result.Metadata
            };

            if (result.Status == Core.Enums.MessageStatus.Failed)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending location message");
            return StatusCode(500, new { error = "Internal server error sending message" });
        }
    }

    /// <summary>
    /// Send an audio message
    /// </summary>
    [HttpPost("audio")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendAudio([FromBody] SendAudioMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Sending audio message for tenant {TenantId} to {To}", tenantId, request.To);

            var audioBytes = Convert.FromBase64String(request.AudioBase64);
            var result = await _messageService.SendAudioAsync(tenantId, request.To, audioBytes, cancellationToken);

            var response = new MessageResponseDto
            {
                MessageId = result.MessageId,
                Status = result.Status,
                Provider = result.Provider,
                Timestamp = result.Timestamp,
                Error = result.Error,
                Metadata = result.Metadata
            };

            if (result.Status == Core.Enums.MessageStatus.Failed)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid base64 audio data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio message");
            return StatusCode(500, new { error = "Internal server error sending message" });
        }
    }

    /// <summary>
    /// Get message status by ID
    /// </summary>
    [HttpGet("{messageId}/status")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMessageStatus(string messageId, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Getting message status for tenant {TenantId}, messageId {MessageId}", tenantId, messageId);

            var result = await _messageService.GetMessageStatusAsync(tenantId, messageId, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Message not found" });
            }

            var response = new MessageResponseDto
            {
                MessageId = result.MessageId,
                Status = result.Status,
                Provider = result.Provider,
                Timestamp = result.Timestamp,
                Error = result.Error,
                Metadata = result.Metadata
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get message history for a contact (conversation)
    /// </summary>
    [HttpGet("history/{phoneNumber}")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMessageHistory(string phoneNumber, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Getting message history for tenant {TenantId}, phone {PhoneNumber}", tenantId, phoneNumber);

            var messages = await _messageService.GetMessageHistoryAsync(tenantId, phoneNumber, limit, cancellationToken);

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all conversations (contacts with messages)
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();

            _logger.LogInformation("Getting conversations for tenant {TenantId}", tenantId);

            var conversations = await _messageService.GetConversationsAsync(tenantId, cancellationToken);

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}