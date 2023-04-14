using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

/// <summary>
/// This represents the REAL AWS account id in the form of a 12-digit number that uniquely identifies an AWS account.
/// This account id is generated by AWS.
/// </summary>
public class RealAwsAccountId : ValueObject
{
    public static readonly RealAwsAccountId Empty = new("");
    
    private readonly string _value;

    private RealAwsAccountId(string value)
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

    public static RealAwsAccountId Parse(string? text)
    {
        if (TryParse(text, out var accountId))
        {
            return accountId;
        }

        throw new FormatException($"Value \"{text}\" is not a valid external aws account id.");
    }

    public static bool TryParse(string? text, out RealAwsAccountId accountId)
    {
        // TODO -- [thfis] allow empty string for now, since we've got those in the database
        if (string.IsNullOrEmpty(text))
        {
            accountId = Empty;
            return true;
        }
        
        if (string.IsNullOrWhiteSpace(text) || !Regex.IsMatch(text, @"\d{12}"))
        {
            accountId = null!;
            return false;
        }

        accountId = new RealAwsAccountId(text);
        return true;
    }

    public static implicit operator RealAwsAccountId(string text) => Parse(text);
    public static implicit operator string(RealAwsAccountId accountId) => accountId.ToString();
}