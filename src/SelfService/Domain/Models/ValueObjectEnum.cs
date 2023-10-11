namespace SelfService.Domain.Models;

public class ValueObjectEnum : ValueObject
{
    private readonly string _value;

    protected virtual Dictionary<string, ValueObjectEnum> AllowedValues => new();

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

    public static bool TryParse(string? text, out ValueObjectEnum role)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            role = null!;
            return false;
        }

        bool success = AllowedValues.TryGetValue(text, out var mappedRole);
        role = success ? mappedRole! : null!;
        return success;
    }

    public static implicit operator string(ValueObjectEnum type) => type.ToString();

    public static ValueObjectEnum Parse(string value)
    {
        TryParse(value, out var result);
        return result;
    }
}
