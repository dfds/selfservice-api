using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    public AwsAccount(AwsAccountId id, CapabilityId capabilityId, DateTime requestedAt, string requestedBy) : base(id)
    {
        CapabilityId = capabilityId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public AwsAccountRegistration Registration { get; private set; } = AwsAccountRegistration.Incomplete;
    public KubernetesLink KubernetesLink { get; private set; } = KubernetesLink.Unlinked;
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }

    public AwsAccountStatus Status
    {
        get
        {
            if (KubernetesLink.LinkedAt is not null)
            {
                return AwsAccountStatus.Completed;
            }
            if (Registration.RegisteredAt is null)
            {
                return AwsAccountStatus.Requested;
            }
            return AwsAccountStatus.Pending;
        }
    }


    public static AwsAccount RequestNew(CapabilityId capabilityId, DateTime requestedAt, string requestedBy)
    {
        var account = new AwsAccount(
            id: AwsAccountId.New(), 
            capabilityId: capabilityId, 
            requestedAt: requestedAt, 
            requestedBy: requestedBy);

        account.Raise(new AwsAccountRequested
        (
            accountId: account.Id
        ));

        return account;
    }

    public void RegisterRealAwsAccount(RealAwsAccountId accountId, string? roleEmail, DateTime registeredAt)
    {
        Registration = new AwsAccountRegistration(accountId, roleEmail, registeredAt);
    }

    public void LinkKubernetesNamespace(string? @namespace, DateTime connectedAt)
    {
        KubernetesLink = new KubernetesLink(@namespace, connectedAt);
    }
}
