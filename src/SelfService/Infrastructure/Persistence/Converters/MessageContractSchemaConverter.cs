using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MessageContractSchemaConverter : ValueConverter<MessageContractSchema, string>
{
    public MessageContractSchemaConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<MessageContractSchema, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, MessageContractSchema>> FromDatabaseType =>
        value => MessageContractSchema.Parse(value);
}
