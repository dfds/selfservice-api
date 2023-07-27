using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class CapabilityIdConverter : ValueConverter<CapabilityId, string>
{
    public CapabilityIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<CapabilityId, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, CapabilityId>> FromDatabaseType => value => CapabilityId.Parse(value);
}