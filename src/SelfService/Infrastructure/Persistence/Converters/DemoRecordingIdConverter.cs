using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class DemoRecordingIdConverter : ValueConverter<DemoRecordingId, Guid>
{
    public DemoRecordingIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<DemoRecordingId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, DemoRecordingId>> FromDatabaseType => value => value;
}
