using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class ReleaseNoteHistoryIdConverter : ValueConverter<ReleaseNoteHistoryId, Guid>
{
    public ReleaseNoteHistoryIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<ReleaseNoteHistoryId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, ReleaseNoteHistoryId>> FromDatabaseType => value => value;
}
