using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaClusterIdConverter : ValueConverter<KafkaClusterId, string>
{
    public KafkaClusterIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaClusterId, string>> ToDatabaseType => id => id;
    private static Expression<Func<string, KafkaClusterId>> FromDatabaseType => value => value;
}