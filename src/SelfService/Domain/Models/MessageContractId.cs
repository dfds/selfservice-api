namespace SelfService.Domain.Models;

public class MessageContractId : ValueObject
{
    private readonly Guid _value;

    private MessageContractId(Guid value)
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

    public static MessageContractId New()
    {
        return new MessageContractId(Guid.NewGuid());
    }

    public static MessageContractId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid message contract id.");
    }

    public static bool TryParse(string? text, out MessageContractId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new MessageContractId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator MessageContractId(string text) => Parse(text);
    public static implicit operator string(MessageContractId id) => id.ToString();

    public static implicit operator MessageContractId(Guid idValue) => new MessageContractId(idValue);
    public static implicit operator Guid(MessageContractId id) => id._value;
}