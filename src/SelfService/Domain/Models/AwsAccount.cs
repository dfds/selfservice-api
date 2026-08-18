using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    public AwsAccount(
        AwsAccountId id,
        CapabilityId capabilityId,
        string environment,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        Environment = environment;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public string Environment { get; private set; }
    public AwsAccountRegistration Registration { get; private set; } = AwsAccountRegistration.Incomplete;
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }

    public AwsAccountStatus Status
    {
        get
        {
            if (Registration.RegisteredAt is null)
            {
                return AwsAccountStatus.Requested;
            }

            return AwsAccountStatus.Completed;
        }
    }

    public static AwsAccount RequestNew(
        CapabilityId capabilityId,
        string environment,
        DateTime requestedAt,
        string requestedBy
    )
    {
        var account = new AwsAccount(
            id: AwsAccountId.New(),
            capabilityId: capabilityId,
            environment: environment,
            requestedAt: requestedAt,
            requestedBy: requestedBy
        );

        account.Raise(new AwsAccountRequested() { AccountId = account.Id, Environment = account.Environment });

        return account;
    }

    public void RegisterRealAwsAccount(RealAwsAccountId accountId, string? roleEmail, DateTime registeredAt)
    {
        Registration = new AwsAccountRegistration(accountId, roleEmail, registeredAt);
    }
}
