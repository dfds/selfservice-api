namespace SelfService.Domain.Models;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DemoIdJsonConverter : JsonConverter<DemoId>
{
    public override DemoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DemoId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, DemoId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

[JsonConverter(typeof(DemoIdJsonConverter))]
public class DemoId : ValueObject
{
    private readonly Guid _value;

    private DemoId(Guid value)
    {
        _value = value;
    }

    public DemoId()
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

    public static DemoId Parse(string? text)
    {
        if (TryParse(text, out var id))
        {
            return id;
        }

        throw new FormatException($"Value \"{text}\" is not a valid DemosRepository id.");
    }

    public static bool TryParse(string? text, out DemoId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new DemoId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator DemoId(string text) => Parse(text);

    public static implicit operator string(DemoId id) => id.ToString();

    public static implicit operator DemoId(Guid idValue) => new DemoId(idValue);

    public static implicit operator Guid(DemoId id) => id._value;
}
