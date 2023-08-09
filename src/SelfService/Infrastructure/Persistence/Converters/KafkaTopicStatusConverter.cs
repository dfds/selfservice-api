using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaTopicStatusConverter : ValueConverter<KafkaTopicStatus, string>
{
    public KafkaTopicStatusConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<KafkaTopicStatus, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, KafkaTopicStatus>> FromDatabaseType =>
        value => KafkaTopicStatus.Parse(value);
}
