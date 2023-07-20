using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class KafkaTopicName : ValueObject
{
    private readonly string _value;

    private KafkaTopicName(string value)
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

    public static bool IsValid(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (!Regex.IsMatch(name, @"[a-zA-Z0-9\._-]"))
        {
            return false;
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z]"))
        {
            return false;
        }

        if (!Regex.IsMatch(name, @"[a-zA-Z0-9\.]$")) // NOTE [jandr@2023-03-01]: legacy data support: we allow that topic name ends with "." but it should be fixed!
        {
            return false;
        }

        if (Regex.IsMatch(name, @"[-_\.]{2,}"))
        {
            return false;
        }

        if (name.Contains(' '))
        {
            return false;
        }

        return true;
    }

    public static KafkaTopicName CreateFrom(CapabilityId capabilityId, string name, bool isPublic = false)
    {
        if (!IsValid(name))
        {
            throw new ArgumentException($"Value \"{name}\" is not valid.", nameof(name));
        }

        var fullName = string.Join(".",
            capabilityId, 
            name
        );

        if (isPublic)
        {
            fullName = "pub." + fullName;
        }

        return new KafkaTopicName(fullName.ToLowerInvariant());
    }

    public static KafkaTopicName Parse(string? text)
    {
        if (TryParse(text, out var name))
        {
            return name;
        }

        throw new FormatException($"Value \"{text}\" is not a valid kafka topic name.");
    }

    public static bool TryParse(string? text, out KafkaTopicName topicName)
    {
        topicName = new KafkaTopicName(text!.ToLowerInvariant());
        return IsValid(text);
    }

    public CapabilityId ExtractCapabilityId()
    {
        var match = Regex.Match(_value, @"^(pub\.)?(?<capability>.*?)\..*?$");
        var capabilityId = match.Groups["capability"].Value;

        return CapabilityId.Parse(capabilityId);
    }

    public static implicit operator string(KafkaTopicName name) => name.ToString();
    public static implicit operator KafkaTopicName(string name) => Parse(name);
}