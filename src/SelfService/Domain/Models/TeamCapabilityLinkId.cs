namespace SelfService.Domain.Models;

public class TeamCapabilityLinkId : ValueObjectGuid<TeamCapabilityLinkId>
{
    private TeamCapabilityLinkId(Guid newGuid)
        : base(newGuid) { }
}
