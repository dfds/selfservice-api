using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class KafkaTopicRetention : ValueObject
{
    public static KafkaTopicRetention OneDay = new KafkaTopicRetention("1d");
    public static KafkaTopicRetention SevenDays = new KafkaTopicRetention("7d");
    public static KafkaTopicRetention ThirtyOneDays = new KafkaTopicRetention("31d");
    public static KafkaTopicRetention OneYear = new KafkaTopicRetention("365d");
    public static KafkaTopicRetention TwoYears = new KafkaTopicRetention("730d");
    public static KafkaTopicRetention Forever = new KafkaTopicRetention("forever");

    private readonly string _value;

    private KafkaTopicRetention(string value)
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

    public static KafkaTopicRetention Parse(string? text)
    {
        if (TryParse(text, out var retention))
        {
            return retention;
        }

        throw new FormatException($"Value \"{text}\" is not a valid/supported topic retention.");
    }

    public static bool TryParse(string? text, out KafkaTopicRetention retention)
    {
        var candidate = text ?? "";

        var match = Regex.Match(candidate, @"^(?<days>\d+)[dD]$");
        if (
            (match.Success && match.Groups["days"].Value != "0")
            || "forever".Equals(candidate, StringComparison.InvariantCultureIgnoreCase)
        )
        {
            retention = new KafkaTopicRetention(text!.ToLowerInvariant());
            return true;
        }

        retention = null!;
        return false;
    }

    public static implicit operator string(KafkaTopicRetention retention) => retention._value;

    public static implicit operator KafkaTopicRetention(string value) => Parse(value);
}
