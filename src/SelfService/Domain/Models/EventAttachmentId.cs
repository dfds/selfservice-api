using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(EventAttachmentIdJsonConverter))]
public class EventAttachmentIdJsonConverter : JsonConverter<EventAttachmentId>
{
    public override EventAttachmentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return EventAttachmentId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, EventAttachmentId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

[JsonConverter(typeof(EventAttachmentIdJsonConverter))]
public class EventAttachmentId : ValueObject
{
    private readonly Guid _value;

    private EventAttachmentId(Guid value)
    {
        _value = value;
    }

    public EventAttachmentId()
    {
        _value = Guid.NewGuid();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString();
    }

    public static EventAttachmentId Parse(string? text)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            return new EventAttachmentId(idValue);
        }

        throw new FormatException($"Value \"{text}\" is not a valid EventAttachment id.");
    }

    public static bool TryParse(string? text, out EventAttachmentId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new EventAttachmentId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator EventAttachmentId(string text) => Parse(text);

    public static implicit operator string(EventAttachmentId id) => id.ToString();

    public static implicit operator EventAttachmentId(Guid idValue) => new EventAttachmentId(idValue);

    public static implicit operator Guid(EventAttachmentId id) => id._value;
}
