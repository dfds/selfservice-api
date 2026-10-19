using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SelfService.Infrastructure.Persistence.Converters;

public class JsonListConverter<T> : ValueConverter<List<T>, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JsonListConverter()
        : base(
            value => JsonSerializer.Serialize(value ?? new List<T>(), JsonOptions),
            value =>
                string.IsNullOrWhiteSpace(value)
                    ? new List<T>()
                    : JsonSerializer.Deserialize<List<T>>(value, JsonOptions) ?? new List<T>()
        ) { }
}
