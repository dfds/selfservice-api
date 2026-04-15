using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Events;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Domain.Services;

public class EventService : IEventService
{
    private readonly ILogger<EventService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IEventAttachmentRepository _eventAttachmentRepository;

    public EventService(
        ILogger<EventService> logger,
        IEventRepository eventRepository,
        IEventAttachmentRepository eventAttachmentRepository
    )
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _eventAttachmentRepository = eventAttachmentRepository;
    }

    public async Task<IEnumerable<Event>> GetAllEvents()
    {
        var events = await _eventRepository.GetAll();

        // Load attachments for each event
        foreach (var eventModel in events)
        {
            var attachments = await _eventAttachmentRepository.GetAttachmentsByEventId(eventModel.Id);
            foreach (var attachment in attachments)
            {
                eventModel.AddAttachment(attachment);
            }
        }

        return events;
    }

    public async Task<Event> GetEventById(EventId eventId)
    {
        var eventModel =
            await _eventRepository.FindById(eventId)
            ?? throw new KeyNotFoundException($"Event with id '{eventId}' not found.");

        // Load attachments
        var attachments = await _eventAttachmentRepository.GetAttachmentsByEventId(eventId);
        foreach (var attachment in attachments)
        {
            eventModel.AddAttachment(attachment);
        }

        return eventModel;
    }

    [TransactionalBoundary]
    public async Task<Event> CreateEvent(Event eventModel)
    {
        await _eventRepository.Add(eventModel);
        return eventModel;
    }

    [TransactionalBoundary]
    public async Task UpdateEvent(EventId eventId, EventUpdateRequest updateRequest)
    {
        var eventModel =
            await _eventRepository.FindById(eventId)
            ?? throw new KeyNotFoundException($"Event with id '{eventId}' not found.");

        eventModel.Update(updateRequest.EventDate, updateRequest.Title, updateRequest.Description, updateRequest.Type);
    }

    [TransactionalBoundary]
    public async Task DeleteEvent(EventId eventId)
    {
        // Simply delete the event - attachments will be cascade deleted by the database
        // due to ON DELETE CASCADE in the EventAttachment_Event_FK foreign key
        await _eventRepository.Remove(eventId);
    }

    public async Task<List<Event>> GetUpcomingEvents(int limit = 10)
    {
        var events = await _eventRepository.GetUpcomingEvents(limit);

        // Load attachments for each event
        foreach (var eventModel in events)
        {
            var attachments = await _eventAttachmentRepository.GetAttachmentsByEventId(eventModel.Id);
            foreach (var attachment in attachments)
            {
                eventModel.AddAttachment(attachment);
            }
        }

        return events;
    }

    public async Task<Event?> GetLatestHeldEvent()
    {
        var eventModel = await _eventRepository.GetLatestHeldEvent();
        if (eventModel != null)
        {
            var attachments = await _eventAttachmentRepository.GetAttachmentsByEventId(eventModel.Id);
            foreach (var attachment in attachments)
            {
                eventModel.AddAttachment(attachment);
            }
        }

        return eventModel;
    }

    [TransactionalBoundary]
    public async Task<EventAttachment> AddAttachmentToEvent(EventId eventId, EventAttachment attachment)
    {
        var eventModel =
            await _eventRepository.FindById(eventId)
            ?? throw new KeyNotFoundException($"Event with id '{eventId}' not found.");

        await _eventAttachmentRepository.Add(attachment);
        return attachment;
    }

    [TransactionalBoundary]
    public async Task DeleteAttachment(EventAttachmentId attachmentId)
    {
        await _eventAttachmentRepository.Remove(attachmentId);
    }
}
