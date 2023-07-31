namespace SelfService.Domain.Models;

public class KafkaTopicId : ValueObject
{
    public static readonly KafkaTopicId None = new(Guid.Empty);
    private readonly Guid _value;

    private KafkaTopicId(Guid value)
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

    public static KafkaTopicId New()
    {
        return new KafkaTopicId(Guid.NewGuid());
    }

    public static KafkaTopicId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid kafka topic id.");
    }

    public static bool TryParse(string? text, out KafkaTopicId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new KafkaTopicId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator KafkaTopicId(string text) => Parse(text);
    public static implicit operator string(KafkaTopicId id) => id.ToString();

    public static implicit operator KafkaTopicId(Guid idValue) => new KafkaTopicId(idValue);
    public static implicit operator Guid(KafkaTopicId id) => id._value;
}
