using System.Reflection;

namespace SelfService.Domain.Models;

public abstract class ValueObjectEnum<TEnumeration> : ValueObject
    where TEnumeration : class
{
    private readonly string _value;
    private static readonly TEnumeration[] Enumerations = GetEnumerations();

    protected ValueObjectEnum(string value)
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

    public static bool TryParse(string? text, out TEnumeration outValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            outValue = null!;
            return false;
        }

        var enums = Enumerations;

        var parsed = enums.FirstOrDefault(x => x?.ToString() == text);
        if (parsed != null)
        {
            outValue = parsed;
            return true;
        }

        outValue = null!;
        return false;
    }

    // NOTE: From https://gist.github.com/spewu/5933739
    private static TEnumeration[] GetEnumerations()
    {
        var enumerationType = typeof(TEnumeration);

        return enumerationType
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(info => enumerationType.IsAssignableFrom(info.FieldType))
            .Select(info => info.GetValue(null))
            .Cast<TEnumeration>()
            .ToArray();
    }

    public static TEnumeration Parse(string value)
    {
        if (TryParse(value, out var result))
        {
            return result;
        }

        throw new FormatException($"Value \"{value}\" is not valid.");
    }
}
