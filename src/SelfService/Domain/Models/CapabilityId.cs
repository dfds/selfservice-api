using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class CapabilityId : ValueObject
{
    private readonly string _value;
    private static readonly Random Random = new Random();

    private const int SuffixSize = 5;
    private const int NameCharLimit = 22;

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

        var name = capabilityName.ToLowerInvariant();
        name = name.Replace("æ", "ae");
        name = name.Replace("ø", "oe");
        name = name.Replace("å", "aa");
        name = Regex.Replace(name, @"\s+", "-");
        name = Regex.Replace(name, @"_+", "-");
        name = Regex.Replace(name, @"-+", "-");
        name = Regex.Replace(name, @"-+$", "");
        name = Regex.Replace(name, @"^-+", "");
        name = Regex.Replace(name, @"[^a-zA-Z0-9\-]", "");
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var preservedNameLength = Math.Min(NameCharLimit, name.Length);
        var suffix = GenerateSuffix();

        var rootId = $"{name[..preservedNameLength]}-{suffix}";
        var capabilityRootId = rootId.ToLowerInvariant();

        id = new CapabilityId(capabilityRootId);
        return true;
    }

    private static string GenerateSuffix()
    {
        const string validCharacters = "abcdefghijklmnopqrstuvwxyz";

        var suffix = new char[SuffixSize];
        for (var i = 0; i < SuffixSize; i++)
        {
            suffix[i] = validCharacters[Random.Next(validCharacters.Length - 1)];
        }

        return new string(suffix);
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