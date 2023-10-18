namespace SelfService.Domain.Models;

public class TeamCapabilityLink : Entity<TeamCapabilityLinkId>
{
    public TeamId TeamId { get; set; }
    public CapabilityId CapabilityId { get; set; }

    public string CreatedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public TeamCapabilityLink(
        TeamCapabilityLinkId id,
        TeamId teamId,
        CapabilityId capabilityId,
        string createdBy,
        DateTime createdAt
    )
        : base(id)
    {
        TeamId = teamId;
        CapabilityId = capabilityId;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public override string ToString()
    {
        return $"Link {TeamId} - {CapabilityId}";
    }
}
