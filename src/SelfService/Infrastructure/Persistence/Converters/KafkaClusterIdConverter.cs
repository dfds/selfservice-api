using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class KafkaClusterIdConverter : ValueConverter<KafkaClusterId, Guid>
{
    public KafkaClusterIdConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<KafkaClusterId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, KafkaClusterId>> FromDatabaseType => value => value;
}