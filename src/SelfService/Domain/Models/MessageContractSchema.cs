namespace SelfService.Domain.Models;

public class MessageContractSchema : ValueObject
{
    private readonly string _value;

    private MessageContractSchema(string value)
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

    public static MessageContractSchema Parse(string? text)
    {
        if (TryParse(text, out var schema))
        {
            return schema;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out MessageContractSchema schema)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            schema = null!;
            return false;
        }

        // NOTE: [jandr] consider having opinions about it being json

        schema = new MessageContractSchema(text!);
        return true;
    }

    public static implicit operator MessageContractSchema(string text) => Parse(text);

    public static implicit operator string(MessageContractSchema schema) => schema.ToString();
}
