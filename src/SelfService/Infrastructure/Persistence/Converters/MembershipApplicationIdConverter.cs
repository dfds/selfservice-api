using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MembershipApplicationIdConverter : ValueConverter<MembershipApplicationId, Guid>
{
    public MembershipApplicationIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<MembershipApplicationId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, MembershipApplicationId>> FromDatabaseType => value => value;
}