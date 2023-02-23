using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class AwsRoleArn : ValueObject
{
    private readonly string _value;

    private AwsRoleArn(string value)
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

    public static AwsRoleArn Parse(string? text)
    {
        if (TryParse(text, out var roleArn))
        {
            return roleArn;
        }

        throw new FormatException($"Value \"{text}\" is not a valid AWS role arn.");
    }

    public static bool TryParse(string? text, out AwsRoleArn roleArn)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            roleArn = null!;
            return false;
        }

        var candidate = text.ToLowerInvariant();
        if (!Regex.IsMatch(candidate, @"^arn:aws:iam(:{1,2})\d{12}\:role/.*?$"))
        {
            roleArn = null!;
            return false;
        }

        roleArn = new AwsRoleArn(candidate);
        return true;
    }

    public static implicit operator AwsRoleArn(string text) => Parse(text);
    public static implicit operator string(AwsRoleArn roleArn) => roleArn.ToString();
}