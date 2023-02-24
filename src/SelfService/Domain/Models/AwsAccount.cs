namespace SelfService.Domain.Models;

public class AwsAccount : AggregateRoot<AwsAccountId>
{
    protected AwsAccount() { }

    public AwsAccount(AwsAccountId id, CapabilityId capabilityId, RealAwsAccountId accountId, AwsRoleArn roleArn, string roleEmail, DateTime createdAt, string createdBy) : base(id)
    {
        AccountId = accountId;
        CapabilityId = capabilityId;
        RoleArn = roleArn;
        RoleEmail = roleEmail;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public RealAwsAccountId AccountId { get; set; }
    public AwsRoleArn RoleArn { get; set; }
    public string RoleEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }

    public static AwsAccount RegisterNew(CapabilityId capabilityId, RealAwsAccountId accountId, AwsRoleArn roleArn, string roleEmail, DateTime createdAt, string createdBy)
    {
        return new AwsAccount(
            id: AwsAccountId.New(),
            capabilityId: capabilityId,
            accountId: accountId,
            roleArn: roleArn,
            roleEmail: roleEmail,
            createdAt: createdAt,
            createdBy: createdBy
        );
    }
}