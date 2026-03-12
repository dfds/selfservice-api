using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ForceUtcKind : ValueConverter<DateTime, DateTime>
{
    public ForceUtcKind()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<DateTime, DateTime>> ToDatabaseType => id => id.ToUniversalTime();
    private static Expression<Func<DateTime, DateTime>> FromDatabaseType => value => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
