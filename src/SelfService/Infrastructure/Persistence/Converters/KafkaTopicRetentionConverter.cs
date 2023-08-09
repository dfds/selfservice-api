using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaTopicRetentionConverter : ValueConverter<KafkaTopicRetention, string>
{
    public KafkaTopicRetentionConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<KafkaTopicRetention, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, KafkaTopicRetention>> FromDatabaseType =>
        value => KafkaTopicRetention.Parse(value);
}
