using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaTopicNameConverter : ValueConverter<KafkaTopicName, string>
{
    public KafkaTopicNameConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaTopicName, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, KafkaTopicName>> FromDatabaseType => value => KafkaTopicName.Parse(value);
}

public class KafkaTopicPartitionsConverter : ValueConverter<KafkaTopicPartitions, uint>
{
    public KafkaTopicPartitionsConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaTopicPartitions, uint>> ToDatabaseType => id => id;
    private static Expression<Func<uint, KafkaTopicPartitions>> FromDatabaseType => value => value;
}

public class KafkaTopicRetentionConverter : ValueConverter<KafkaTopicRetention, string>
{
    public KafkaTopicRetentionConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaTopicRetention, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, KafkaTopicRetention>> FromDatabaseType => value => KafkaTopicRetention.Parse(value);
}