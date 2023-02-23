using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SelfService.Infrastructure.Persistence.Converters;

public class RealAwsAccountIdConverter : ValueConverter<RealAwsAccountId, string>
{
    public RealAwsAccountIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<RealAwsAccountId, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, RealAwsAccountId>> FromDatabaseType => value => RealAwsAccountId.Parse(value);
}