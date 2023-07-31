using System.Text.RegularExpressions;

namespace SelfService.Domain.Models;

public class MessageType : ValueObject
{
    private readonly string _value;

    private MessageType(string value)
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

    public static MessageType Parse(string? text)
    {
        if (TryParse(text, out var result))
        {
            return result;
        }

        throw new FormatException($"Value \"{text}\" is not valid.");
    }

    public static bool TryParse(string? text, out MessageType messageType)
    {
        messageType = null!;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (Regex.IsMatch(text ?? "", @"^\s+"))
        {
            return false;
        }

        if (Regex.IsMatch(text ?? "", @"\s+$"))
        {
            return false;
        }

        if (Regex.IsMatch(text ?? "", @"^[_-]"))
        {
            return false;
        }

        if (Regex.IsMatch(text ?? "", @"[_-]$"))
        {
            return false;
        }

        if (Regex.IsMatch(text ?? "", @"[^a-zA-Z0-9_-]"))
        {
            return false;
        }

        messageType = new MessageType(text!.ToLower());
        return true;
    }

    public static implicit operator MessageType(string text) => Parse(text);

    public static implicit operator string(MessageType messageType) => messageType.ToString();
}
