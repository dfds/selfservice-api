using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class KubernetesAccess : AggregateRoot<KubernetesAccessId>
{
    public KubernetesAccess(
        KubernetesAccessId id,
        CapabilityId capabilityId,
        string environment,
        AwsAccountId? awsAccountId,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        Environment = environment;
        AwsAccountId = awsAccountId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public string Environment { get; private set; }

    // nullable: required while K8s access depends on an AWS account, removable when fully decoupled
    public AwsAccountId? AwsAccountId { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }
    public string? Namespace { get; private set; }
    public DateTime? GrantedAt { get; private set; }

    public KubernetesAccessStatus Status =>
        GrantedAt is null ? KubernetesAccessStatus.Requested : KubernetesAccessStatus.Active;

    public static KubernetesAccess Request(
        CapabilityId capabilityId,
        string environment,
        AwsAccountId? awsAccountId,
        DateTime requestedAt,
        string requestedBy
    )
    {
        var access = new KubernetesAccess(
            id: KubernetesAccessId.New(),
            capabilityId: capabilityId,
            environment: environment,
            awsAccountId: awsAccountId,
            requestedAt: requestedAt,
            requestedBy: requestedBy
        );

        access.Raise(
            new KubernetesAccessRequested
            {
                KubernetesAccessId = access.Id.ToString(),
                AwsAccountId = awsAccountId?.ToString(),
                CapabilityId = capabilityId.ToString(),
                Environment = environment,
                RequestedAt = requestedAt,
                RequestedBy = requestedBy,
            }
        );

        return access;
    }

    public void GrantAccess(string @namespace, DateTime grantedAt)
    {
        Namespace = @namespace;
        GrantedAt = grantedAt;
    }
}
