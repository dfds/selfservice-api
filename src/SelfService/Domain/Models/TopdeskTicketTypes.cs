namespace SelfService.Domain.Models;

public class TopdeskTicketType : ValueObject
{
    public static readonly TopdeskTicketType awsAccountRequest = new("AWS_ACCOUNT_REQUEST");
    public static readonly TopdeskTicketType capabilityDeletionRequest = new("CAPABILITY_DELETION_REQUEST");

    protected TopdeskTicketType(string type)
    {
        Type = type;
    }

    public string Type { get; private set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type!;
    }

    public override string ToString()
    {
        return Type;
    }

    public static implicit operator string(TopdeskTicketType type) => type.ToString();
}
