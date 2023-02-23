namespace SelfService.Domain.Models;

public class AwsAccountId : ValueObject
{
    private readonly Guid _value;

    private AwsAccountId(Guid value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString("N");
    }

    public static AwsAccountId New() => new AwsAccountId(Guid.NewGuid());

    public static AwsAccountId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid AWS account id.");
    }

    public static bool TryParse(string? text, out AwsAccountId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new AwsAccountId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator AwsAccountId(string text) => Parse(text);
    public static implicit operator string(AwsAccountId id) => id.ToString();
    public static implicit operator AwsAccountId(Guid idValue) => new AwsAccountId(idValue);
    public static implicit operator Guid(AwsAccountId id) => id._value;
}