namespace SelfService.Domain.Events;

public class KubernetesAccessRequested : IDomainEvent
{
    public const string EventType = "kubernetes-access-requested";

    public string? AccountId { get; set; }
    public string? CapabilityId { get; set; }
    public string? CapabilityRootId { get; set; }
    public string? ContextId { get; set; }
    public string? NamespaceName { get; set; }
}
