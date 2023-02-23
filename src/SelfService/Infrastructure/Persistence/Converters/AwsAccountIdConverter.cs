using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class AwsAccountIdConverter : ValueConverter<AwsAccountId, Guid>
{
    public AwsAccountIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<AwsAccountId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, AwsAccountId>> FromDatabaseType => value => value;
}