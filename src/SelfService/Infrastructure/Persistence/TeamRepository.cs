using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

// Extend GenericRepository to get the basic CRUD operations
// Implement ITeamRepository for dependency injection
public class TeamRepository : GenericRepository<Team, TeamId>, ITeamRepository
{
    public TeamRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Teams) { }
}
