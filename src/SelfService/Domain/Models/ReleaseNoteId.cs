namespace SelfService.Domain.Models;

public class ReleaseNoteId : ValueObject
{
    private readonly Guid _value;

    private ReleaseNoteId(Guid value)
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

    public static ReleaseNoteId New() => new ReleaseNoteId(Guid.NewGuid());

    public static ReleaseNoteId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Release Note id.");
    }

    public static bool TryParse(string? text, out ReleaseNoteId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new ReleaseNoteId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ReleaseNoteId(string text) => Parse(text);

    public static implicit operator string(ReleaseNoteId id) => id.ToString();

    public static implicit operator ReleaseNoteId(Guid idValue) => new ReleaseNoteId(idValue);

    public static implicit operator Guid(ReleaseNoteId id) => id._value;
}
