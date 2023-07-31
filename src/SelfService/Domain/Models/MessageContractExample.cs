namespace SelfService.Domain.Models;

public class MessageContractExample : ValueObject
{
    private readonly string _value;

    private MessageContractExample(string value)
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

    public static MessageContractExample Parse(string? text)
    {
        if (TryParse(text, out var example))
        {
            return example;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out MessageContractExample example)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            example = null!;
            return false;
        }

        // NOTE: [jandr] consider having opinions about it being json

        example = new MessageContractExample(text!);
        return true;
    }

    public static implicit operator MessageContractExample(string text) => Parse(text);

    public static implicit operator string(MessageContractExample example) => example.ToString();
}
