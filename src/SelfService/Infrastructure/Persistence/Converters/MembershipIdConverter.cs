using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MembershipIdConverter : ValueConverter<MembershipId, Guid>
{
    public MembershipIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<MembershipId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, MembershipId>> FromDatabaseType => value => value;
}