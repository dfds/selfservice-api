using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class MessageContractIdConverter : ValueConverter<MessageContractId, Guid>
{
    public MessageContractIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<MessageContractId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, MessageContractId>> FromDatabaseType => value => value;
}
