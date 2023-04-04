using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    protected AwsAccount() { }

    public AwsAccount(AwsAccountId id, CapabilityId capabilityId, RealAwsAccountId? accountId, string? roleEmail, DateTime createdAt, string createdBy) : base(id)
    {
        AccountId = accountId;
        CapabilityId = capabilityId;
        RoleEmail = roleEmail;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public CapabilityId CapabilityId { get; private set; } = null!;
    public RealAwsAccountId? AccountId { get; set; }
    public string? RoleEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public static AwsAccount RequestNew(CapabilityId capabilityId, DateTime createdAt, string requestedBy)
    {
        var account = new AwsAccount(
            id: AwsAccountId.New(),
            capabilityId: capabilityId,
            accountId: null,
            roleEmail: null,
            createdAt: createdAt,
            createdBy: requestedBy);

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