using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class FavouriteIdConverter : ValueConverter<FavouriteId, Guid>
{
    public FavouriteIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<FavouriteId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, FavouriteId>> FromDatabaseType => value => value;
}
