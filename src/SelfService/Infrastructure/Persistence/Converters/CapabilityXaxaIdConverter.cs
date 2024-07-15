using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class CapabilityXaxaIdConverter : ValueConverter<CapabilityXaxaId, Guid>
{
    public CapabilityXaxaIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<CapabilityXaxaId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, CapabilityXaxaId>> FromDatabaseType => value => value;
}
