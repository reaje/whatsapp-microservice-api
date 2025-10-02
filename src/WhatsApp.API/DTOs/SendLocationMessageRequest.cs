using System.ComponentModel.DataAnnotations;

namespace WhatsApp.API.DTOs;

public class SendLocationMessageRequest
{
    [Required]
    [Phone]
    public string To { get; set; } = string.Empty;

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }
}