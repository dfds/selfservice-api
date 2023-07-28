namespace SelfService.Domain.Models;

public class MembershipApplicationStatusOptions: ValueObject
{
    public static readonly MembershipApplicationStatusOptions Unknown = new("Unknown");
    public static readonly MembershipApplicationStatusOptions PendingApprovals = new("PendingApprovals");
    public static readonly MembershipApplicationStatusOptions Finalized = new("Finalized");
    public static readonly MembershipApplicationStatusOptions Cancelled = new("Cancelled");

    private readonly string _value;

    private MembershipApplicationStatusOptions(string value)
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

    public static MembershipApplicationStatusOptions Parse(string? text)
    {
        if (TryParse(text, out var status))
        {
            return status;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out MembershipApplicationStatusOptions status)
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

    public static IReadOnlyCollection<MembershipApplicationStatusOptions> Values => new[]
    {
        Unknown, PendingApprovals, Finalized, Cancelled
    };

    public static implicit operator MembershipApplicationStatusOptions(string text)
        => Parse(text);

    public static implicit operator string(MembershipApplicationStatusOptions status)
        => status.ToString();
}
