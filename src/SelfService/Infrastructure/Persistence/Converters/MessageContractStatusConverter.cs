using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MessageContractStatusConverter : ValueConverter<MessageContractStatus, string>
{
    public MessageContractStatusConverter() : base(ToDatabaseType, FromDatabaseType)
    {
            
    }

    private static Expression<Func<MessageContractStatus, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, MessageContractStatus>> FromDatabaseType => value => MessageContractStatus.Parse(value);
}