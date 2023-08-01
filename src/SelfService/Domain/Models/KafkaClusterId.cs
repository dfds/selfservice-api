namespace SelfService.Domain.Models;

public class KafkaClusterId : ValueObject
{
    private readonly string _value;

    private KafkaClusterId(string value)
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
        if (!string.IsNullOrWhiteSpace(text))
        {
            id = new KafkaClusterId(text);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator KafkaClusterId(string text) => Parse(text);

    public static implicit operator string(KafkaClusterId id) => id.ToString();
}
