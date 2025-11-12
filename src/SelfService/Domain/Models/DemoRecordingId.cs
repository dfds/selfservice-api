namespace SelfService.Domain.Models;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DemoIdJsonConverter : JsonConverter<DemoRecordingId>
{
    public override DemoRecordingId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DemoRecordingId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DemoRecordingId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

[JsonConverter(typeof(DemoIdJsonConverter))]
public class DemoRecordingId : ValueObject
{
    private readonly Guid _value;

    private DemoRecordingId(Guid value)
    {
        _value = value;
    }

    public DemoRecordingId()
    {
        _value = Guid.NewGuid();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _value;
    }

    public override string ToString()
    {
        return _value.ToString("N");
    }

    public static DemoRecordingId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid DemosRecording id.");
    }

    public static bool TryParse(string? text, out DemoRecordingId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new DemoRecordingId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator DemoRecordingId(string text) => Parse(text);

    public static implicit operator string(DemoRecordingId id) => id.ToString();

    public static implicit operator DemoRecordingId(Guid idValue) => new DemoRecordingId(idValue);

    public static implicit operator Guid(DemoRecordingId id) => id._value;
}
