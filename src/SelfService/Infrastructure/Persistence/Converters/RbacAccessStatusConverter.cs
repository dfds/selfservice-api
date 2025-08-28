using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Configuration;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class RbacAccessTypeConverter : ValueConverter<RbacAccessType, string>
{
    public RbacAccessTypeConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<RbacAccessType, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, RbacAccessType>> FromDatabaseType =>
        value => RbacAccessType.Parse(value);
}
