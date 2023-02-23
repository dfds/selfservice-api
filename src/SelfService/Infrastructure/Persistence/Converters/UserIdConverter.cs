using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class UserIdConverter : ValueConverter<UserId, string>
{
    public UserIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<UserId, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, UserId>> FromDatabaseType => value => UserId.Parse(value);
}