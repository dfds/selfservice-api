namespace SelfService.Domain.Models;

public class UserId : ValueObject
{
    private readonly string _value;

    private UserId(string value)
    {
        _value = value.ToLowerInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static UserId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" for user id not valid.");
    }

    public static bool TryParse(string? text, out UserId id)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            id = null!;
            return false;
        }

        id = new UserId(text);
        return true;
    }

    public static implicit operator UserId(string text) => Parse(text);
    public static implicit operator string(UserId id) => id.ToString();
}