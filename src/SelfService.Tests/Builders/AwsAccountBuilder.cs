using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class AwsAccountBuilder
{
    private AwsAccountId _id;
    private CapabilityId _capabilityId;
    private RealAwsAccountId _accountId;
    private AwsRoleArn _roleArn;
    private string _roleEmail;
    private DateTime _createdAt;
    private string _createdBy;

    public AwsAccountBuilder()
    {
        _id = AwsAccountId.New();
        _capabilityId = CapabilityId.Parse("foo");
        _accountId = RealAwsAccountId.Parse(new string('0', 12));
        _roleArn = AwsRoleArn.Parse($"arn:aws:iam::{_accountId}:role/foo");
        _roleEmail = "foo@foo.com";
        _createdAt = new DateTime(2000, 1, 1);
        _createdBy = nameof(AwsAccountBuilder);
    }

    public AwsAccount Build()
    {
        return new AwsAccount(
            id: _id,
            capabilityId: _capabilityId,
            accountId: _accountId,
            roleEmail: _roleEmail,
            requestedAt: _createdAt,
            requestedBy: _createdBy
        );
    }

    public static implicit operator AwsAccount(AwsAccountBuilder builder)
        => builder.Build();
}