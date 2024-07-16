namespace SelfService.Domain.Models;

public class CapabilityClaimId : ValueObject
{
    private readonly Guid _value;

    public CapabilityClaimId(Guid value)
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
    
    public static CapabilityClaimId New() => new CapabilityClaimId(Guid.NewGuid());

    public static CapabilityClaimId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Capability Claim id.");
    }

    public static bool TryParse(string? text, out CapabilityClaimId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new CapabilityClaimId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator CapabilityClaimId(string text) => Parse(text);

    public static implicit operator string(CapabilityClaimId id) => id.ToString();

    public static implicit operator CapabilityClaimId(Guid idValue) => new CapabilityClaimId(idValue);

    public static implicit operator Guid(CapabilityClaimId id) => id._value;
}