namespace SelfService.Domain.Models;

public class TeamId : ValueObjectGuid<TeamId>
{
    private TeamId(Guid id)
        : base(id) { }
}
