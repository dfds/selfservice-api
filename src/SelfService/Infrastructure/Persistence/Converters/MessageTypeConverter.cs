using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MessageTypeConverter : ValueConverter<MessageType, string>
{
    public MessageTypeConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<MessageType, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, MessageType>> FromDatabaseType => value => MessageType.Parse(value);
}
