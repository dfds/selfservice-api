using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class AzureResourceRequested : IDomainEvent
{
    public const string EventType = "azure-resource-requested";

    public string? AzureResourceId { get; set; }
    public string? Purpose { get; set; }
    public string? CatalogueId { get; set; }
    public string? Risk { get; set; }
    public bool? Gdpr { get; set; }
}
