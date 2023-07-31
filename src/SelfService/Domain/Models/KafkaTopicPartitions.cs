namespace SelfService.Domain.Models;

public class KafkaTopicPartitions : ValueObject
{
    public static KafkaTopicPartitions One = new(1);
    public static KafkaTopicPartitions Three = new(3);
    public static KafkaTopicPartitions Six = new(6);

    private readonly uint _value;

    private KafkaTopicPartitions(uint value)
    {
        _value = value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString();
    }

    public uint ToValue() => _value;

    public static KafkaTopicPartitions From(uint value)
    {
        if (TryCreate(value, out var partitions))
        {
            return partitions;
        }

        throw new ArgumentOutOfRangeException(nameof(value), $"Value \"{value}\" is not supported as partition count.");
    }

    public static bool TryCreate(uint value, out KafkaTopicPartitions partitions)
    {
        var supportedValues = new uint[] { One, Three, Six };

        if (supportedValues.Contains(value))
        {
            partitions = new KafkaTopicPartitions(value);
            return true;
        }

        partitions = null!;
        return false;
    }

    public static implicit operator KafkaTopicPartitions(uint value) => new(value);

    public static implicit operator uint(KafkaTopicPartitions partitions) => partitions._value;
}
