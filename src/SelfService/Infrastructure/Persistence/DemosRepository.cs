using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class DemosRepository : GenericRepository<Demo, DemoId>, IDemosRepository
{
    public DemosRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Demos) { }
}
