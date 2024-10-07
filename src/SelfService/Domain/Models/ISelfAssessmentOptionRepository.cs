namespace SelfService.Domain.Models;

public interface ISelfAssessmentOptionRepository
{
    Task AddSelfAssessmentOption(SelfAssessmentOption option);

    Task<List<SelfAssessmentOption>> GetAllSelfAssessmentOptions();

    Task<SelfAssessmentOption?> Get(SelfAssessmentOptionId id);

    Task DeactivateSelfAssessmentOption(SelfAssessmentOptionId id);

    Task ActivateSelfAssessmentOption(SelfAssessmentOptionId id);

    Task UpdateSelfAssessmentOption(
        SelfAssessmentOptionId id,
        string? shortName,
        string? description,
        string? documentationUrl
    );
}
