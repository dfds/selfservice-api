namespace SelfService.Domain.Models;

public interface ISelfAssessmentRepository
{
    Task AddSelfAssessment(SelfAssessment assessment);
    Task UpdateSelfAssessment(SelfAssessment assessment);

    Task<bool> SelfAssessmentExists(CapabilityId capabilityId, SelfAssessmentOptionId selfAssessmentOptionId);

    Task<SelfAssessment?> GetSpecificSelfAssessmentForCapability(
        CapabilityId capabilityId,
        SelfAssessmentOptionId selfAssessmentOptionId
    );

    Task<List<SelfAssessment>> GetSelfAssessmentsForCapability(CapabilityId capabilityId);
}
