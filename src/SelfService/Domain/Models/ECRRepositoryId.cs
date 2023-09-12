namespace SelfService.Domain.Models;

public class ECRRepositoryId : ValueObject
{
    private readonly Guid _value;

    private ECRRepositoryId(Guid value)
    {
        _value = value;
    }

    public ECRRepositoryId()
    {
        _value = Guid.NewGuid();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString("N");
    }

    public static ECRRepositoryId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid ECRRepository id.");
    }

    public static bool TryParse(string? text, out ECRRepositoryId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new ECRRepositoryId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator ECRRepositoryId(string text) => Parse(text);

    public static implicit operator string(ECRRepositoryId id) => id.ToString();

    public static implicit operator ECRRepositoryId(Guid idValue) => new ECRRepositoryId(idValue);

    public static implicit operator Guid(ECRRepositoryId id) => id._value;
}
