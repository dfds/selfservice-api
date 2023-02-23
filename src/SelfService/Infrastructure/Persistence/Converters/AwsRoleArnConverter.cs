using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class AwsRoleArnConverter : ValueConverter<AwsRoleArn, string>
{
    public AwsRoleArnConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<AwsRoleArn, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, AwsRoleArn>> FromDatabaseType => value => AwsRoleArn.Parse(value);
}