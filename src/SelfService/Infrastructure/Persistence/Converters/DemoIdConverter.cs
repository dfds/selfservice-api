using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class DemoIdConverter : ValueConverter<DemoId, Guid>
{
    public DemoIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<DemoId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, DemoId>> FromDatabaseType => value => value;
}
