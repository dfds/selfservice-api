using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class UserRole : ValueObject
{
    public static readonly UserRole CloudEngineer = new("CloudEngineer");
    public static readonly UserRole NormalUser = new("NormalUser");
    
    private readonly string _value;

    public UserRole(string value)
    {
        _value = value;
    }

    public static IReadOnlyCollection<UserRole> Values => new[]
    {
        CloudEngineer, NormalUser
    };

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static bool TryParse(string? text, out UserRole role)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            role = null!;
            return false;
        }

        if (Regex.IsMatch(text, @"^\s*Cloud[-_\.\s]Engineer\s*$"))
        {
            role = CloudEngineer;
        }
        else
        {
            role = NormalUser;
        }

        return true;
    }

    public static UserRole Parse(string? text)
    {
        if (TryParse(text, out var role))
        {
            return role;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static implicit operator UserRole(string text)
        => Parse(text);

    public static implicit operator string(UserRole role)
        => role.ToString();
}