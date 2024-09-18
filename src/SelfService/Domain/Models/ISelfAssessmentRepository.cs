namespace SelfService.Domain.Models;

public interface ISelfAssessmentRepository
{
    Task AddSelfAssessment(SelfAssessment assessment);

    Task<bool> SelfAssessmentExists(CapabilityId capabilityId, string assessmentType);

    Task<SelfAssessment?> GetSpecificSelfAssessmentForCapability(CapabilityId capabilityId, string assessmentType);

    Task RemoveSelfAssessment(SelfAssessment assessment);

    Task<List<SelfAssessment>> GetSelfAssessmentsForCapability(CapabilityId capabilityId);

    List<SelfAssessmentOption> ListPossibleSelfAssessments();
}
