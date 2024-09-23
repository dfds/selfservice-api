using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class SelfAssessmentIdConverter : ValueConverter<SelfAssessmentId, Guid>
{
    public SelfAssessmentIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<SelfAssessmentId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, SelfAssessmentId>> FromDatabaseType => value => value;
}
