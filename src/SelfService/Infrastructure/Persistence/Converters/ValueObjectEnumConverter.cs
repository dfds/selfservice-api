using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ValueObjectEnumConverter<T> : ValueConverter<T, string>
    where T : ValueObjectEnum
{
    public ValueObjectEnumConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<T, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, T>> FromDatabaseType => value => (T)ValueObjectEnum.Parse(value);
}
