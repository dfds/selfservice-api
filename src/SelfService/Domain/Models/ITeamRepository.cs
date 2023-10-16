namespace SelfService.Domain.Models;

public interface ITeamRepository : IGenericRepository<Team, TeamId>
{
    Task<Team?> FindByName(string name);
}
