using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class EventAttachmentIdConverter : ValueConverter<EventAttachmentId, Guid>
{
    public EventAttachmentIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<EventAttachmentId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, EventAttachmentId>> FromDatabaseType => value => value;
}
