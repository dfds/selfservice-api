namespace SelfService.Domain.Models;

public class KafkaTopicStatus : ValueObject
{
    public static readonly KafkaTopicStatus Requested = new("Requested");
    public static readonly KafkaTopicStatus InProgress = new("In Progress");
    public static readonly KafkaTopicStatus Provisioned = new("Provisioned");

    private readonly string _value;

    private KafkaTopicStatus(string requested)
    {
        _value = requested;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value;
    }

    public static KafkaTopicStatus Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid kafka topic status.");
    }

    public static bool TryParse(string? input, out KafkaTopicStatus kafkaTopicStatus)
    {

        switch (input)
        {
            case "Requested":
                kafkaTopicStatus = Requested;
                break;
            case "In Progress":
                kafkaTopicStatus = InProgress;
                break;
            case "Provisioned":
                kafkaTopicStatus = Provisioned;
                break;
            default:
                kafkaTopicStatus = null!;
                return false;
        }

        return true;
    }
}