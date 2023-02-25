namespace SelfService.Domain.Models;

public class MembershipApplicationId : ValueObject
{
    private readonly Guid _value;

    private MembershipApplicationId(Guid value)
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

    public static MembershipApplicationId New()
    {
        return new MembershipApplicationId(Guid.NewGuid());
    }

    public static MembershipApplicationId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid membership application id.");
    }

    public static bool TryParse(string? text, out MembershipApplicationId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new MembershipApplicationId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator MembershipApplicationId(string text) => Parse(text);
    public static implicit operator string(MembershipApplicationId id) => id.ToString();

    public static implicit operator MembershipApplicationId(Guid idValue) => new MembershipApplicationId(idValue);
    public static implicit operator Guid(MembershipApplicationId id) => id._value;
}