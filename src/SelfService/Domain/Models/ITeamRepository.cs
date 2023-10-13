namespace SelfService.Domain.Models;

// TODO: Replace with generic repository
public interface ITeamRepository
{
    Task<Team?> FindBy(TeamId id);
    Task<Team?> FindByName(string name);
    Task<bool> Exists(TeamId id);
    Task Add(Team team);
    Task<IEnumerable<Team>> GetAll();
    void Remove(TeamId id);
}
