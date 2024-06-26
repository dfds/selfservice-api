using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class AzureResourceRequested : IDomainEvent
{
    public const string EventType = "azure-resource-requested";

    public string? AzureResourceId { get; set; }
}
