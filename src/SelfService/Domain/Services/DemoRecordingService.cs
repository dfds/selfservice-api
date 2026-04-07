using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;
using EventId = SelfService.Domain.Models.EventId;

namespace SelfService.Domain.Services;

public class DemoRecordingService : IDemoRecordingService
{
    private readonly ILogger<DemoRecordingService> _logger;
    private readonly IDemoRecordingRepository _demoRecordingsRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventAttachmentRepository _eventAttachmentRepository;

    public DemoRecordingService(
        ILogger<DemoRecordingService> logger,
        IDemoRecordingRepository demoRecordingsRepository,
        IEventRepository eventRepository,
        IEventAttachmentRepository eventAttachmentRepository
    )
    {
        _logger = logger;
        _demoRecordingsRepository = demoRecordingsRepository;
        _eventRepository = eventRepository;
        _eventAttachmentRepository = eventAttachmentRepository;
    }

    public async Task<IEnumerable<DemoRecording>> GetAllDemoRecordings()
    {
        return await _demoRecordingsRepository.GetAll();
    }

    public async Task<DemoRecording> GetDemoRecordingById(DemoRecordingId demoId)
    {
        var demo =
            await _demoRecordingsRepository.FindById(demoId)
            ?? throw new KeyNotFoundException($"Demo with id '{demoId}' not found.");
        return demo;
    }

    [TransactionalBoundary]
    public async Task<DemoRecording> AddDemoRecording(DemoRecording demo)
    {
        await _demoRecordingsRepository.Add(demo);

        // Also create a corresponding Event
        var eventModel = new Event(
            id: EventId.Parse(demo.Id.ToString()),
            eventDate: demo.RecordingDate,
            title: demo.Title,
            description: demo.Description,
            type: EventType.Demo,
            createdBy: demo.CreatedBy,
            createdAt: demo.CreatedAt
        );

        await _eventRepository.Add(eventModel);

        // Add recording URL as an attachment
        if (!string.IsNullOrWhiteSpace(demo.RecordingUrl))
        {
            var recordingAttachment = new EventAttachment(
                id: new EventAttachmentId(),
                eventId: eventModel.Id,
                url: demo.RecordingUrl,
                attachmentType: EventAttachmentType.Recording,
                description: "Demo recording",
                createdAt: demo.CreatedAt
            );
            await _eventAttachmentRepository.Add(recordingAttachment);
        }

        // Add slides URL as an attachment
        if (!string.IsNullOrWhiteSpace(demo.SlidesUrl))
        {
            var slidesAttachment = new EventAttachment(
                id: new EventAttachmentId(),
                eventId: eventModel.Id,
                url: demo.SlidesUrl,
                attachmentType: EventAttachmentType.Document,
                description: "Demo slides",
                createdAt: demo.CreatedAt
            );
            await _eventAttachmentRepository.Add(slidesAttachment);
        }

        _logger.LogInformation(
            "Created Event {EventId} with attachments for DemoRecording {DemoId}",
            eventModel.Id,
            demo.Id
        );

        return demo;
    }

    [TransactionalBoundary]
    public async Task UpdateDemoRecording(DemoRecordingId demoId, DemoRecordingUpdateRequest updateRequest)
    {
        var demo =
            await _demoRecordingsRepository.FindById(demoId)
            ?? throw new KeyNotFoundException($"Demo with id '{demoId}' not found.");

        demo.Update(updateRequest);
    }

    [TransactionalBoundary]
    public async Task DeleteDemoRecording(DemoRecordingId demoId)
    {
        await _demoRecordingsRepository.Remove(demoId);
    }
}
