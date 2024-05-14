using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class AzureResourceIdConverter : ValueConverter<AzureResourceId, Guid>
{
    public AzureResourceIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<AzureResourceId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, AzureResourceId>> FromDatabaseType => value => value;
}
