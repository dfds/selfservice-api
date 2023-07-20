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
    private static Expression<Func<string, KafkaTopicName>> FromDatabaseType => value => Parse(value);
    
    private static KafkaTopicName Parse(string value)
    {
        KafkaTopicName.TryParse(value, out var payload); // TODO: Consider adding additional logging if validation fails
        return payload;
    }
}
