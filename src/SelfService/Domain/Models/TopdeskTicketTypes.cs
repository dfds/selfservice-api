namespace SelfService.Domain.Models;

public class TopdeskTicketType : ValueObject
{
    public static readonly TopdeskTicketType AwsAccountRequest = new("AWS_ACCOUNT_REQUEST");
    public static readonly TopdeskTicketType CapabilityDeletionRequest = new("CAPABILITY_DELETION_REQUEST");

    private TopdeskTicketType(string type)
    {
        _value = type;
    }

    private readonly string _value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static implicit operator string(TopdeskTicketType type) => type.ToString();
}
