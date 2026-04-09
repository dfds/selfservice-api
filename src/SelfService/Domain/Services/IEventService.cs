using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Events;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Domain.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllEvents();
    Task<Event> GetEventById(EventId eventId);
    Task<Event> CreateEvent(Event eventModel);
    Task UpdateEvent(EventId eventId, EventUpdateRequest updateRequest);
    Task DeleteEvent(EventId eventId);
    Task<List<Event>> GetUpcomingEvents(int limit = 10);
    Task<Event?> GetLatestHeldEvent();
    Task<EventAttachment> AddAttachmentToEvent(EventId eventId, EventAttachment attachment);
    Task DeleteAttachment(EventAttachmentId attachmentId);
}
