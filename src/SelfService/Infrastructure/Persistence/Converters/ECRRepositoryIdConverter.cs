using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ECRRepositoryIdConverter : ValueConverter<ECRRepositoryId, Guid>
{
    public ECRRepositoryIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<ECRRepositoryId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, ECRRepositoryId>> FromDatabaseType => value => value;
}
