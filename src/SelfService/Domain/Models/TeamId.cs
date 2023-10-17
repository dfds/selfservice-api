namespace SelfService.Domain.Models;

public class TeamId : ValueObjectGuid<TeamId>
{
    private TeamId(Guid id)
        : base(id) { }

    public static implicit operator TeamId(Guid idValue) => new TeamId(idValue);
}
