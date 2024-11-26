namespace SelfService.Domain.Models;

public class SelfAssessmentStatus : ValueObject
{
    public static readonly SelfAssessmentStatus NotApplicable = new("Not Applicable");
    public static readonly SelfAssessmentStatus Satisfied = new("Satisfied");
    public static readonly SelfAssessmentStatus Violated = new("Violated");

    private readonly string _value;

    private SelfAssessmentStatus(string value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static SelfAssessmentStatus Parse(string? text)
    {
        if (TryParse(text, out var status))
        {
            return status;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out SelfAssessmentStatus status)
    {
        var input = text ?? "";

        foreach (var value in Values)
        {
            if (value.ToString().Equals(input, StringComparison.InvariantCultureIgnoreCase))
            {
                status = value;
                return true;
            }
        }

        status = null!;
        return false;
    }

    public static IReadOnlyCollection<SelfAssessmentStatus> Values => new[] { Satisfied, Violated, NotApplicable };

    public static implicit operator SelfAssessmentStatus(string text) => Parse(text);

    public static implicit operator string(SelfAssessmentStatus status) => status.ToString();
}
