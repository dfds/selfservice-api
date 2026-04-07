namespace SelfService.Domain.Models;

public interface IEventAttachmentRepository : IGenericRepository<EventAttachment, EventAttachmentId>
{
    Task<List<EventAttachment>> GetAttachmentsByEventId(EventId eventId);
}
