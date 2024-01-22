using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Kafka;

public class ValidateMessageContractRequest
{
    [Required]
    public string? MessageType { get; set; }

    [Required]
    public string? Schema { get; set; }
}
