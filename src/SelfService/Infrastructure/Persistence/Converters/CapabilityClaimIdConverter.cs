using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class CapabilityClaimIdConverter : ValueConverter<CapabilityClaimId, Guid>
{
    public CapabilityClaimIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<CapabilityClaimId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, CapabilityClaimId>> FromDatabaseType => value => value;
}
