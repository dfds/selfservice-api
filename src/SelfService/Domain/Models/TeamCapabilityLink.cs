namespace SelfService.Domain.Models;

public class TeamCapabilityLink : Entity<Guid>
{
    public TeamId TeamId { get; set; }
    public CapabilityId CapabilityId { get; set; }

    public TeamCapabilityLink(TeamId teamId, CapabilityId capabilityId)
    {
        TeamId = teamId;
        CapabilityId = capabilityId;
    }

    public override string ToString()
    {
        return $"Link {TeamId} - {CapabilityId}";
    }
}
