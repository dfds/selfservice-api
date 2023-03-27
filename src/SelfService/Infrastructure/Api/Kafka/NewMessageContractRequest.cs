using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Kafka;

public class NewMessageContractRequest
{
    [Required]
    public string? MessageType { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? Example { get; set; }
    
    [Required]
    public string? Schema { get; set; }
}