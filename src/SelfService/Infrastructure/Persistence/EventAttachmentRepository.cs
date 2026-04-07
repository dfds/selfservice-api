using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Infrastructure.Persistence;

public class EventAttachmentRepository
    : GenericRepository<EventAttachment, EventAttachmentId>,
        IEventAttachmentRepository
{
    public EventAttachmentRepository(SelfServiceDbContext dbContext)
        : base(dbContext.EventAttachments) { }

    public async Task<List<EventAttachment>> GetAttachmentsByEventId(EventId eventId)
    {
        return await DbSetReference.Where(a => a.EventId == eventId).OrderBy(a => a.CreatedAt).ToListAsync();
    }
}
