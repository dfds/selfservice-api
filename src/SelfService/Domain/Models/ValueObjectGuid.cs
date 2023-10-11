namespace SelfService.Domain.Models;

public class ValueObjectGuid<T> : ValueObject
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

    public static ValueObjectGuid<T> Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid guid.");
    }

    public static bool TryParse(string? text, out ValueObjectGuid<T> id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new ValueObjectGuid<T>(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ValueObjectGuid<T>(string text) => Parse(text);

    public static implicit operator string(ValueObjectGuid<T> id) => id.ToString();

    public static implicit operator ValueObjectGuid<T>(Guid idValue) => new(idValue);

    public static implicit operator Guid(ValueObjectGuid<T> id) => id.Id;
}
