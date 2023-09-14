namespace SelfService.Domain.Models;

public class SelfServiceJsonSchemaObjectId : ValueObject
{
    public static readonly SelfServiceJsonSchemaObjectId Capability = new("CAPABILITY");

    private static readonly HashSet<SelfServiceJsonSchemaObjectId> _validValues = new() { Capability };

    private SelfServiceJsonSchemaObjectId(string id)
    {
        _value = id;
    }

    private readonly string _value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public static bool TryParse(string? text, out SelfServiceJsonSchemaObjectId accountId)
    {
        accountId = null!;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (!_validValues.TryGetValue(new SelfServiceJsonSchemaObjectId(text), out var parsedValue))
            return false;
        accountId = parsedValue;
        return true;
    }

    public override string ToString()
    {
        return _value;
    }

    public static implicit operator string(SelfServiceJsonSchemaObjectId type) => type.ToString();
}
