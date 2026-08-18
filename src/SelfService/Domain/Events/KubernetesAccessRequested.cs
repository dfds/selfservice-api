using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class KubernetesAccessRequested : IDomainEvent
{
    public const string EventType = "kubernetes-access-requested";

    public string? KubernetesAccessId { get; set; }
    public string? AwsAccountId { get; set; }
    public string? CapabilityId { get; set; }
    public string? Environment { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? RequestedBy { get; set; }
}
