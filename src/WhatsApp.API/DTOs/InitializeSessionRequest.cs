using System.ComponentModel.DataAnnotations;
using WhatsApp.Core.Enums;

namespace WhatsApp.API.DTOs;

public class InitializeSessionRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public ProviderType ProviderType { get; set; } = ProviderType.Baileys;
}