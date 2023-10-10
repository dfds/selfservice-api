using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ValueObjectGuidConverter<T> : ValueConverter<T, Guid>
    where T : ValueObjectGuid
{
    public ValueObjectGuidConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<T, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, T>> FromDatabaseType => value => (T)value;
}

public class TeamIdConverter : ValueConverter<TeamId, Guid>
{
    public TeamIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<TeamId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, TeamId>> FromDatabaseType => value => TeamId.Parse(value.ToString());
}
