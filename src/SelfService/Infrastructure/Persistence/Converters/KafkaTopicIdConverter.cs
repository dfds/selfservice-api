using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaTopicIdConverter : ValueConverter<KafkaTopicId, Guid>
{
    public KafkaTopicIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaTopicId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, KafkaTopicId>> FromDatabaseType => value => value;
}