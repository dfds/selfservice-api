namespace SelfService.Domain.Models;

public class SelfAssessmentOptionId : ValueObject
{
    private readonly Guid _value;

    public SelfAssessmentOptionId(Guid value)
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

    public static SelfAssessmentOptionId New() => new(Guid.NewGuid());

    public static SelfAssessmentOptionId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid Capability Self Assessment Option id.");
    }

    public static bool TryParse(string? text, out SelfAssessmentOptionId id)
    {
        if (Guid.TryParse(text, out var selfAssessmentOptionId))
        {
            id = new SelfAssessmentOptionId(selfAssessmentOptionId);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator SelfAssessmentOptionId(string text) => Parse(text);

    public static implicit operator string(SelfAssessmentOptionId id) => id.ToString();

    public static implicit operator SelfAssessmentOptionId(Guid idValue) => new(idValue);

    public static implicit operator Guid(SelfAssessmentOptionId id) => id._value;
}
