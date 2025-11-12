using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DemoRecordingServiceBuilder
{
    private IDemoRecordingRepository _demoRecordingsRepository;

    public DemoRecordingServiceBuilder()
    {
        _demoRecordingsRepository = Dummy.Of<IDemoRecordingRepository>();
    }

    public DemoRecordingServiceBuilder WithDemoRecordingRepository(IDemoRecordingRepository demoRecordingRepository)
    {
        _demoRecordingsRepository = demoRecordingRepository;
        return this;
    }

    public DemoRecordingService Build()
    {
        return new DemoRecordingService(Mock.Of<ILogger<DemoRecordingService>>(), _demoRecordingsRepository);
    }

    public static implicit operator DemoRecordingService(DemoRecordingServiceBuilder builder) => builder.Build();
}
