using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DemoRecordingServiceBuilder
{
    private IDemoRecordingRepository _demoRecordingsRepository;
    private IEventRepository _eventRepository;
    private IEventAttachmentRepository _eventAttachmentRepository;

    public DemoRecordingServiceBuilder()
    {
        _demoRecordingsRepository = Dummy.Of<IDemoRecordingRepository>();
        _eventRepository = Dummy.Of<IEventRepository>();
        _eventAttachmentRepository = Dummy.Of<IEventAttachmentRepository>();
    }

    public DemoRecordingServiceBuilder WithDemoRecordingRepository(IDemoRecordingRepository demoRecordingRepository)
    {
        _demoRecordingsRepository = demoRecordingRepository;
        return this;
    }

    public DemoRecordingServiceBuilder WithEventRepository(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
        return this;
    }

    public DemoRecordingServiceBuilder WithEventAttachmentRepository(
        IEventAttachmentRepository eventAttachmentRepository
    )
    {
        _eventAttachmentRepository = eventAttachmentRepository;
        return this;
    }

    public DemoRecordingService Build()
    {
        return new DemoRecordingService(
            Mock.Of<ILogger<DemoRecordingService>>(),
            _demoRecordingsRepository,
            _eventRepository,
            _eventAttachmentRepository
        );
    }

    public static implicit operator DemoRecordingService(DemoRecordingServiceBuilder builder) => builder.Build();
}
