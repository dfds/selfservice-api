namespace SelfService.Domain.Models;

public class TeamCapabilityAssociation : Entity<Guid>
{
    public TeamId TeamId { get; set; }
    public CapabilityId CapabilityId { get; set; }

    public TeamCapabilityAssociation(TeamId teamId, CapabilityId capabilityId)
    {
        TeamId = teamId;
        CapabilityId = capabilityId;
    }

    public override string ToString()
    {
        return $"Association {TeamId} - {CapabilityId}";
    }
}
