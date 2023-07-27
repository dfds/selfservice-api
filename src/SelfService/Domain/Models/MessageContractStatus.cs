namespace SelfService.Domain.Models;

public class MessageContractStatus : ValueObject
{
    public static readonly MessageContractStatus Requested = new("Requested");
    public static readonly MessageContractStatus InProgress = new("In Progress");
    public static readonly MessageContractStatus Provisioned = new("Provisioned");

    private readonly string _value;

    private MessageContractStatus(string value)
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

    public static MessageContractStatus Parse(string? text)
    {
        if (TryParse(text, out var status))
        {
            return status;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out MessageContractStatus status)
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

    public static IReadOnlyCollection<MessageContractStatus> Values => new[]
    {
        Requested, InProgress, Provisioned
    };

    public static implicit operator MessageContractStatus(string text) 
        => Parse(text);

    public static implicit operator string(MessageContractStatus status) 
        => status.ToString();
}