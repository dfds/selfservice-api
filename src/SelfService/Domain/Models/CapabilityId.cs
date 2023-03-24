using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class CapabilityId : ValueObject
{
    private readonly string _value;

    private CapabilityId(string value)
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

    public static CapabilityId CreateFrom(string capabilityName)
    {
        if (TryCreateFrom(capabilityName, out var capabilityId))
        {
            return capabilityId;
        }
        throw new FormatException($"The value \"{capabilityName}\" is not valid.");
    }

    public static bool TryCreateFrom(string? capabilityName, out CapabilityId id)
    {
        id = null!; //TODO [paulseghers] null object pattern for CapabilityId & logic for this
        if (string.IsNullOrWhiteSpace(capabilityName))
        {
            return false;
        }

        var value = capabilityName.ToLowerInvariant();
        value = value.Replace("æ", "ae");
        value = value.Replace("ø", "oe");
        value = value.Replace("å", "aa");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"_+", "-");
        value = Regex.Replace(value, @"-+$", "");
        value = Regex.Replace(value, @"^-+", "");
        value = Regex.Replace(value, @"[^a-zA-Z0-9\-]", "");
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        
        id = new CapabilityId(value);
        return true;
    }


    public static CapabilityId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out CapabilityId id)
    {
        if (!string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, @"[a-zA-Z0-9\-]"))
        {
            id = new CapabilityId(text);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator CapabilityId(string text) => CreateFrom(text);
    public static implicit operator string(CapabilityId id) => id.ToString();
}