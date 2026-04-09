using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Infrastructure.Persistence;

public class EventRepository : GenericRepository<Event, EventId>, IEventRepository
{
    public EventRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Events) { }

    public async Task<List<Event>> GetUpcomingEvents(int limit = 10)
    {
        var now = DateTime.UtcNow;
        var events = await DbSetReference
            .Where(e => e.EventDate > now)
            .OrderBy(e => e.EventDate)
            .Take(limit)
            .ToListAsync();

        return events;
    }

    public async Task<Event?> GetLatestHeldEvent()
    {
        var now = DateTime.UtcNow;
        var latestEvent = await DbSetReference
            .Where(e => e.EventDate <= now)
            .OrderByDescending(e => e.EventDate)
            .FirstOrDefaultAsync();

        return latestEvent;
    }
}
