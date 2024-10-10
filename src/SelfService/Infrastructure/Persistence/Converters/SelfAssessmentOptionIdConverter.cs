using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Converters;

public class SelfAssessmentOptionIdConverter : ValueConverter<SelfAssessmentOptionId, Guid>
{
    public SelfAssessmentOptionIdConverter()
        : base(ToDatabaseType, FromDatabaseType) { }

    private static Expression<Func<SelfAssessmentOptionId, Guid>> ToDatabaseType => id => id;
    private static Expression<Func<Guid, SelfAssessmentOptionId>> FromDatabaseType => value => value;
}
