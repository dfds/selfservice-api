using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MessageContractExampleConverter : ValueConverter<MessageContractExample, string>
{
    public MessageContractExampleConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<MessageContractExample, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, MessageContractExample>> FromDatabaseType => value => MessageContractExample.Parse(value);
}