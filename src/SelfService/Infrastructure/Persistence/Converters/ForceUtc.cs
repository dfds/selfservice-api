using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ForceUtc : ValueConverter<DateTime, DateTime>
{
    public ForceUtc() : base(ToDatabaseType, FromDatabaseType)
    {

    }

    private static Expression<Func<DateTime, DateTime>> ToDatabaseType => id => id.ToUniversalTime();
    private static Expression<Func<DateTime, DateTime>> FromDatabaseType => value => value.ToLocalTime();
}