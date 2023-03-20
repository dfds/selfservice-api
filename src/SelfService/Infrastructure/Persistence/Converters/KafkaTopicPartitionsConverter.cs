using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaTopicPartitionsConverter : ValueConverter<KafkaTopicPartitions, uint>
{
    public KafkaTopicPartitionsConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaTopicPartitions, uint>> ToDatabaseType => id => id;
    private static Expression<Func<uint, KafkaTopicPartitions>> FromDatabaseType => value => value;
}