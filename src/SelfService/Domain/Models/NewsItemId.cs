using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfService.Domain.Models;

[JsonConverter(typeof(NewsItemIdJsonConverter))]
public class NewsItemIdJsonConverter : JsonConverter<NewsItemId>
{
    public override NewsItemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return NewsItemId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, NewsItemId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

[JsonConverter(typeof(NewsItemIdJsonConverter))]
public class NewsItemId : ValueObject
{
    private readonly Guid _value;

    private NewsItemId(Guid value)
    {
        _value = value;
    }

    public NewsItemId()
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

    public static NewsItemId Parse(string? text)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            return new NewsItemId(idValue);
        }

        throw new FormatException($"Value \"{text}\" is not a valid NewsItem id.");
    }

    public static bool TryParse(string? text, out NewsItemId id)
    {
        if (Guid.TryParse(text, out var idValue))
        {
            id = new NewsItemId(idValue);
            return true;
        }

        id = null!;
        return false;
    }

    public static implicit operator NewsItemId(string text) => Parse(text);

    public static implicit operator string(NewsItemId id) => id.ToString();

    public static implicit operator NewsItemId(Guid idValue) => new NewsItemId(idValue);

    public static implicit operator Guid(NewsItemId id) => id._value;
}
