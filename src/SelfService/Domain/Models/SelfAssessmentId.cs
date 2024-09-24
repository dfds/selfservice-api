namespace SelfService.Domain.Models;

public class SelfAssessmentId : ValueObject
{
    private readonly Guid _value;

    public SelfAssessmentId(Guid value)
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

    public static SelfAssessmentId New() => new(Guid.NewGuid());

    public static SelfAssessmentId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Capability Self Assessment id.");
    }

    public static bool TryParse(string? text, out SelfAssessmentId id)
    {
        if (Guid.TryParse(text, out var accountId))
        {
            id = new SelfAssessmentId(accountId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator SelfAssessmentId(string text) => Parse(text);

    public static implicit operator string(SelfAssessmentId id) => id.ToString();

    public static implicit operator SelfAssessmentId(Guid idValue) => new(idValue);

    public static implicit operator Guid(SelfAssessmentId id) => id._value;
}
