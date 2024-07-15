namespace SelfService.Domain.Models;

public class CapabilityXaxaId : ValueObject
{
    private readonly Guid _value;

    private CapabilityXaxaId(Guid value)
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

    public static CapabilityXaxaId New() => new CapabilityXaxaId(Guid.NewGuid());

    public static CapabilityXaxaId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Capability xaxa id.");
    }

    public static bool TryParse(string? text, out CapabilityXaxaId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new CapabilityXaxaId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator CapabilityXaxaId(string text) => Parse(text);

    public static implicit operator string(CapabilityXaxaId id) => id.ToString();

    public static implicit operator CapabilityXaxaId(Guid idValue) => new CapabilityXaxaId(idValue);

    public static implicit operator Guid(CapabilityXaxaId id) => id._value;
}
