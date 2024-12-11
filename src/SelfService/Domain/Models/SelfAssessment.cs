namespace SelfService.Domain.Models;

public class SelfAssessment : AggregateRoot<SelfAssessmentId>
{
    public SelfAssessment(
        SelfAssessmentId id,
        SelfAssessmentOptionId optionId,
        string shortName,
        CapabilityId capabilityId,
        DateTime requestedAt,
        string requestedBy,
        string status
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        OptionId = optionId;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        ShortName = shortName;
        Status = status;
    }

    public SelfAssessmentOptionId OptionId { get; private set; }
    public CapabilityId CapabilityId { get; private set; }
    public string ShortName { get; private set; }
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; private set; }
    public string Status { get; set; }
}
