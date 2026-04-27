using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class NewsItemIdConverter : ValueConverter<NewsItemId, Guid>
{
    public NewsItemIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<NewsItemId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, NewsItemId>> FromDatabaseType => value => value;
}
