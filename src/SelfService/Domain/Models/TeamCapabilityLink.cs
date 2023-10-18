namespace SelfService.Domain.Models;

public class TeamCapabilityLink : Entity<Guid>
{
    public TeamId TeamId { get; set; }
    public CapabilityId CapabilityId { get; set; }

    public string CreatedBy { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public TeamCapabilityLink(
        Guid linkId,
        TeamId teamId,
        CapabilityId capabilityId,
        string createdBy,
        DateTime createdAt
    )
        : base(linkId)
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
