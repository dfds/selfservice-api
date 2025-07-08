namespace SelfService.Domain.Models;

public class ReleaseNoteHistoryId : ValueObject
{
    private readonly Guid _value;

    private ReleaseNoteHistoryId(Guid value)
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

    public static ReleaseNoteHistoryId New() => new ReleaseNoteHistoryId(Guid.NewGuid());

    public static ReleaseNoteHistoryId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Release Note id.");
    }

    public static bool TryParse(string? text, out ReleaseNoteHistoryId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new ReleaseNoteHistoryId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ReleaseNoteHistoryId(string text) => Parse(text);

    public static implicit operator string(ReleaseNoteHistoryId id) => id.ToString();

    public static implicit operator ReleaseNoteHistoryId(Guid idValue) => new ReleaseNoteHistoryId(idValue);

    public static implicit operator Guid(ReleaseNoteHistoryId id) => id._value;
}
