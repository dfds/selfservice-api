using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ReleaseNoteIdConverter : ValueConverter<ReleaseNoteId, Guid>
{
    public ReleaseNoteIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<ReleaseNoteId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, ReleaseNoteId>> FromDatabaseType => value => value;
}
