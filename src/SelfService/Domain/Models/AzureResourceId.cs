namespace SelfService.Domain.Models;

public class AzureResourceId : ValueObject
{
    private readonly Guid _value;

    private AzureResourceId(Guid value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString("D");
    }

    public static AzureResourceId New() => new AzureResourceId(Guid.NewGuid());

    public static AzureResourceId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Azure Resource id.");
    }

    public static bool TryParse(string? text, out AzureResourceId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new AzureResourceId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator AzureResourceId(string text) => Parse(text);

    public static implicit operator string(AzureResourceId id) => id.ToString();

    public static implicit operator AzureResourceId(Guid idValue) => new AzureResourceId(idValue);

    public static implicit operator Guid(AzureResourceId id) => id._value;
}
