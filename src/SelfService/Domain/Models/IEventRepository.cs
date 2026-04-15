namespace SelfService.Domain.Models;

public interface IEventRepository : IGenericRepository<Event, EventId>
{
    Task<List<Event>> GetUpcomingEvents(int limit = 10);
    Task<Event?> GetLatestHeldEvent();
}
