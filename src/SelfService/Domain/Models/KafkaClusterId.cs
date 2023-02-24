namespace SelfService.Domain.Models;

public class KafkaClusterId : ValueObject
{
    private readonly Guid _value;

    private KafkaClusterId(Guid value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString("N");
    }

    public static KafkaClusterId New()
    {
        return new KafkaClusterId(Guid.NewGuid());
    }

    public static KafkaClusterId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid kafka cluster id.");
    }

    public static bool TryParse(string? text, out KafkaClusterId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new KafkaClusterId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator KafkaClusterId(string text) => Parse(text);
    public static implicit operator string(KafkaClusterId id) => id.ToString();

    public static implicit operator KafkaClusterId(Guid idValue) => new KafkaClusterId(idValue);
    public static implicit operator Guid(KafkaClusterId id) => id._value;
}