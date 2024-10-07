namespace SelfService.Domain.Models;

public interface ISelfAssessmentRepository
{
    Task AddSelfAssessment(SelfAssessment assessment);

    Task<bool> SelfAssessmentExists(CapabilityId capabilityId, SelfAssessmentOptionId selfAssessmentOptionId);

    Task<SelfAssessment?> GetSpecificSelfAssessmentForCapability(
        CapabilityId capabilityId,
        SelfAssessmentOptionId selfAssessmentOptionId
    );

    Task RemoveSelfAssessment(SelfAssessment assessment);

    Task<List<SelfAssessment>> GetSelfAssessmentsForCapability(CapabilityId capabilityId);
}
