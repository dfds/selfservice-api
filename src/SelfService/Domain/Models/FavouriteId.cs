namespace SelfService.Domain.Models;

public class FavouriteId : ValueObject
{
    private readonly Guid _value;

    private FavouriteId(Guid value)
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

    public static FavouriteId New()
    {
        return new FavouriteId(Guid.NewGuid());
    }

    public static FavouriteId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid favourite id.");
    }

    public static bool TryParse(string? text, out FavouriteId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new FavouriteId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator FavouriteId(string text) => Parse(text);

    public static implicit operator string(FavouriteId id) => id.ToString();

    public static implicit operator FavouriteId(Guid idValue) => new FavouriteId(idValue);

    public static implicit operator Guid(FavouriteId id) => id._value;
}
