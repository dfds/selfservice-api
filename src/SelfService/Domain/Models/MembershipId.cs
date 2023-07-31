namespace SelfService.Domain.Models;

public class MembershipId : ValueObject
{
    private readonly Guid _value;

    private MembershipId(Guid value)
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

    public static MembershipId New()
    {
        return new MembershipId(Guid.NewGuid());
    }

    public static MembershipId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid membership id.");
    }

    public static bool TryParse(string? text, out MembershipId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new MembershipId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator MembershipId(string text) => Parse(text);

    public static implicit operator string(MembershipId id) => id.ToString();

    public static implicit operator MembershipId(Guid idValue) => new MembershipId(idValue);

    public static implicit operator Guid(MembershipId id) => id._value;
}
