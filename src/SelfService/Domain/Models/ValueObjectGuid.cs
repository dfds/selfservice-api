namespace SelfService.Domain.Models;

public class ValueObjectGuid : ValueObject
{
    public Guid Id { get; protected set; }

    protected ValueObjectGuid(Guid newGuid)
    {
        Id = newGuid;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }

    public override string ToString()
    {
        return Id.ToString("N");
    }

    public static ValueObjectGuid Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid guid.");
    }

    public static bool TryParse(string? text, out ValueObjectGuid id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new ValueObjectGuid(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ValueObjectGuid(string text) => Parse(text);

    public static implicit operator string(ValueObjectGuid id) => id.ToString();

    public static implicit operator ValueObjectGuid(Guid idValue) => new(idValue);

    public static implicit operator Guid(ValueObjectGuid id) => id.Id;
}
