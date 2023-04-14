namespace SelfService.Domain.Models;

public class AwsAccountRegistration : ValueObject
{
    public static readonly AwsAccountRegistration Incomplete = new();

    protected AwsAccountRegistration()
    {
    }

    public AwsAccountRegistration(RealAwsAccountId accountId, string? roleEmail, DateTime registeredAt)
    {
        AccountId = accountId;
        RoleEmail = roleEmail;
        RegisteredAt = registeredAt;
    }

    public RealAwsAccountId? AccountId { get; private set; }
    public string? RoleEmail { get; private set; }
    public DateTime? RegisteredAt { get; private set; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return AccountId!;
        yield return RoleEmail!;
        yield return RegisteredAt!;
    }

    public override string ToString()
    {
        return AccountId is null ? "<incomplete>" : AccountId.ToString();
    }
}