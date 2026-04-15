using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(EventIdJsonConverter))]
public class EventIdJsonConverter : JsonConverter<EventId>
{
    public override EventId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return EventId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, EventId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

[JsonConverter(typeof(EventIdJsonConverter))]
public class EventId : ValueObject
{
    private readonly Guid _value;

    private EventId(Guid value)
    {
        _value = value;
    }

    public EventId()
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

    public static EventId Parse(string? text)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            return new EventId(idValue);
        }

        throw new FormatException($"Value \"{text}\" is not a valid Event id.");
    }

    public static bool TryParse(string? text, out EventId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new EventId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator EventId(string text) => Parse(text);

    public static implicit operator string(EventId id) => id.ToString();

    public static implicit operator EventId(Guid idValue) => new EventId(idValue);

    public static implicit operator Guid(EventId id) => id._value;
}
