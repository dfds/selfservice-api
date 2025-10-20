using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Configuration;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class RbacNamespaceConverter : ValueConverter<RbacNamespace, string>
{
    public RbacNamespaceConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<RbacNamespace, string>> ToDatabaseType => id => id.ToString();
    private static Expression<Func<string, RbacNamespace>> FromDatabaseType => value => RbacNamespace.Parse(value);
}
