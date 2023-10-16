using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class TeamRepository : GenericRepository<Team, TeamId>, ITeamRepository
{
    public TeamRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Teams) { }

    public Task<Team?> FindByName(string name)
    {
        return _dbSetReference.FirstOrDefaultAsync(t => t.Name == name);
    }
}
