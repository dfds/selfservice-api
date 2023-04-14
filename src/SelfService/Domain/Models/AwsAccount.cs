using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    protected AwsAccount()
    {
    }

    public AwsAccount(AwsAccountId id, CapabilityId capabilityId, DateTime requestedAt, string requestedBy) : base(id)
    {
        CapabilityId = capabilityId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
    }

    public CapabilityId CapabilityId { get; private set; } = null!;
    public AwsAccountRegistration Registration { get; private set; } = AwsAccountRegistration.Incomplete;
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; set; } = null!;

    public DateTime? CompletedAt { get; set; }

    public AwsAccountStatus Status
    {
        get
        {
            if (CompletedAt is not null)
            {
                return AwsAccountStatus.Completed;
            }
            if (Registration.RegisteredAt is null)
            {
                return AwsAccountStatus.Pending;
            }
            return AwsAccountStatus.Registered;
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
        {
            AccountId = account.Id
        });

        return account;
    }

    public void RegisterRealAwsAccount(RealAwsAccountId accountId, string? roleEmail, DateTime registeredAt)
    {
        Registration = new AwsAccountRegistration(accountId, roleEmail, registeredAt);
    }

    public void Complete(DateTime completedAt)
    {
        CompletedAt = completedAt;
    }
}