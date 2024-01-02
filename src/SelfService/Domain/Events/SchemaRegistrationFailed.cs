using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class SchemaRegistrationFailed : IDomainEvent
{
    public string? MessageContractId { get; set; }
    public string? Reason { get; set; }
}
