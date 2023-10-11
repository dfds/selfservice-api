using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ValueObjectGuidConverter<T> : ValueConverter<T, Guid>
    where T : ValueObjectGuid<T>
{
    public ValueObjectGuidConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<T, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, T>> FromDatabaseType => value => (T)value;
}
