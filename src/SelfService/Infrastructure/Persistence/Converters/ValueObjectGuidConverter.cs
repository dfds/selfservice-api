using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ValueObjectGuidConverter<T> : ValueConverter<T, string>
    where T : ValueObjectGuid<T>
{
    public ValueObjectGuidConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<T, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, T>> FromDatabaseType => value => ValueObjectGuid<T>.Parse(value);
}
