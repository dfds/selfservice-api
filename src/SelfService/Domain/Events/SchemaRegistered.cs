using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class SchemaRegistered : IDomainEvent
{
    public string? MessageContractId { get; set; }
}
