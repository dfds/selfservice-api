using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Events;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Domain.Services;

public class EventService : IEventService
{
    private readonly ILogger<EventService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IEventAttachmentRepository _eventAttachmentRepository;
    private readonly IPrivateAssetRepository _privateAssetRepository;

    public EventService(
        ILogger<EventService> logger,
        IEventRepository eventRepository,
        IEventAttachmentRepository eventAttachmentRepository,
        IPrivateAssetRepository privateAssetRepository
    )
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _eventAttachmentRepository = eventAttachmentRepository;
        _privateAssetRepository = privateAssetRepository;
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
    public async Task<Event> CreateEvent(Event eventModel, EventCreateRequest request, string userId)
    {
        await _privateAssetRepository.Claim(request.AssetIds ?? [], PrivateAssetOwnerType.Event, eventModel.Id, userId);
        await _eventRepository.Add(eventModel);
        foreach (var attachmentDto in request.Attachments ?? [])
        {
            var attachment = new EventAttachment(
                id: new EventAttachmentId(),
                eventId: eventModel.Id,
                url: attachmentDto.Url!,
                attachmentType: attachmentDto.Type,
                description: attachmentDto.Description,
                createdAt: DateTime.UtcNow
            );
            await _eventAttachmentRepository.Add(attachment);
            eventModel.AddAttachment(attachment);
        }
        return eventModel;
    }

    [TransactionalBoundary]
    public async Task UpdateEvent(EventId eventId, EventUpdateRequest updateRequest, string userId)
    {
        var eventModel =
            await _eventRepository.FindById(eventId)
            ?? throw new KeyNotFoundException($"Event with id '{eventId}' not found.");

        await _privateAssetRepository.Claim(updateRequest.AssetIds ?? [], PrivateAssetOwnerType.Event, eventId, userId);
        eventModel.Update(updateRequest.EventDate, updateRequest.Title, updateRequest.Description, updateRequest.Type);

        if (updateRequest.Attachments is null)
            return;

        var currentAttachments = await _eventAttachmentRepository.GetAttachmentsByEventId(eventId);
        var requestedIds = updateRequest
            .Attachments.Where(attachment => attachment.Id is not null)
            .Select(attachment => attachment.Id!)
            .ToHashSet();
        foreach (var attachment in currentAttachments)
        {
            if (!requestedIds.Contains(attachment.Id))
                await _eventAttachmentRepository.Remove(attachment.Id);
        }

        foreach (
            var attachmentDto in updateRequest.Attachments.Where(attachment =>
                attachment.Id is not null && attachment.Url is not null
            )
        )
        {
            var attachment =
                await _eventAttachmentRepository.FindById(attachmentDto.Id!)
                ?? throw new KeyNotFoundException($"Attachment with id '{attachmentDto.Id}' not found.");
            attachment.Update(attachmentDto.Url!, attachmentDto.Type, attachmentDto.Description);
        }

        foreach (var attachmentDto in updateRequest.Attachments.Where(attachment => attachment.Id is null))
        {
            await _eventAttachmentRepository.Add(
                new EventAttachment(
                    id: new EventAttachmentId(),
                    eventId: eventId,
                    url: attachmentDto.Url!,
                    attachmentType: attachmentDto.Type,
                    description: attachmentDto.Description,
                    createdAt: DateTime.UtcNow
                )
            );
        }
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
    public async Task UpdateAttachment(
        EventAttachmentId attachmentId,
        string url,
        EventAttachmentType type,
        string? description
    )
    {
        var attachment =
            await _eventAttachmentRepository.FindById(attachmentId)
            ?? throw new KeyNotFoundException($"Attachment with id '{attachmentId}' not found.");

        attachment.Update(url, type, description);
    }

    [TransactionalBoundary]
    public async Task DeleteAttachment(EventAttachmentId attachmentId)
    {
        await _eventAttachmentRepository.Remove(attachmentId);
    }
}
