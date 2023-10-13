using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

// TODO: Replace with generic repository
public class TeamRepository : ITeamRepository
{
    public Task<Team> Get(TeamId id)
    {
        throw new NotImplementedException();
    }

    public Task<Team?> FindBy(TeamId id)
    {
        throw new NotImplementedException();
    }

    public Task<Team?> FindByName(string name)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Exists(TeamId id)
    {
        throw new NotImplementedException();
    }

    public Task Add(Team team)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Team>> GetAll()
    {
        throw new NotImplementedException();
    }

    public void Remove(TeamId id)
    {
        throw new NotImplementedException();
    }
}
