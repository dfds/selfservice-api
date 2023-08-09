using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class CapabilityStatusOptionsConverter : ValueConverter<CapabilityStatusOptions, string>
{
    public CapabilityStatusOptionsConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<CapabilityStatusOptions, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, CapabilityStatusOptions>> FromDatabaseType =>
        value => CapabilityStatusOptions.Parse(value);
}
