using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    protected AwsAccount() { }

    public AwsAccount(AwsAccountId id, CapabilityId capabilityId, RealAwsAccountId? accountId, string? roleEmail, DateTime requestedAt, string requestedBy) : base(id)
    {
        AccountId = accountId;
        CapabilityId = capabilityId;
        RoleEmail = roleEmail;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
    }

    public CapabilityId CapabilityId { get; private set; } = null!;
    public RealAwsAccountId? AccountId { get; set; }
    public string? RoleEmail { get; set; }
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; set; } = null!;

    public static AwsAccount RequestNew(CapabilityId capabilityId, DateTime requestedAt, string requestedBy)
    {
        var account = new AwsAccount(
            id: AwsAccountId.New(),
            capabilityId: capabilityId,
            accountId: null,
            roleEmail: null,
            requestedAt: requestedAt,
            requestedBy: requestedBy);

        account.Raise(new AwsAccountRequested
        {
            AccountId = account.Id
        });
        
        return account;
    }

    public static AwsAccount RegisterNew(CapabilityId capabilityId, RealAwsAccountId accountId, AwsRoleArn roleArn, string roleEmail, DateTime createdAt, string createdBy)
    {
        return new AwsAccount(
            id: AwsAccountId.New(),
            capabilityId: capabilityId,
            accountId: accountId,
            roleEmail: roleEmail,
            createdAt: createdAt,
            createdBy: createdBy
        );
    }
}