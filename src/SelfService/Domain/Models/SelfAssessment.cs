namespace SelfService.Domain.Models;

public class SelfAssessment : AggregateRoot<SelfAssessmentId>
{
    public SelfAssessment(
        SelfAssessmentId id,
        string selfAssessmentType,
        CapabilityId capabilityId,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        SelfAssessmentType = selfAssessmentType;
    }

    public CapabilityId CapabilityId { get; private set; }
    public string SelfAssessmentType { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }
}
