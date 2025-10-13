using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class UserSettingsConverter : ValueConverter<UserSettings, string>
{
    public UserSettingsConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static readonly JsonSerializerOptions _jsonOptions = new();

    private static readonly Expression<Func<UserSettings, string>> ToDatabaseType = value =>
        JsonSerializer.Serialize(value, _jsonOptions);

    private static readonly Expression<Func<string, UserSettings>> FromDatabaseType = value =>
        JsonSerializer.Deserialize<UserSettings>(value, _jsonOptions) ?? new UserSettings();
}
