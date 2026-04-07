using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Infrastructure.Persistence.Converters;

public class EventIdConverter : ValueConverter<EventId, Guid>
{
    public EventIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<EventId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, EventId>> FromDatabaseType => value => value;
}
