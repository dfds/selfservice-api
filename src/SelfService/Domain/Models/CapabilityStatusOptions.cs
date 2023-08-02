namespace SelfService.Domain.Models;

public class CapabilityStatusOptions : ValueObject
{
    public static readonly CapabilityStatusOptions Active = new("Active");
    public static readonly CapabilityStatusOptions PendingDeletion = new("Pending Deletion");
    public static readonly CapabilityStatusOptions Deleted = new("Deleted");

    private readonly string _value;

    private CapabilityStatusOptions(string value)
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

    public static CapabilityStatusOptions Parse(string? text)
    {
        if (TryParse(text, out var status))
        {
            return status;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out CapabilityStatusOptions status)
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

    public static IReadOnlyCollection<CapabilityStatusOptions> Values =>
        new[] { Active, PendingDeletion, Deleted };

    public static implicit operator CapabilityStatusOptions(string text) => Parse(text);

    public static implicit operator string(CapabilityStatusOptions status) => status.ToString();
}
