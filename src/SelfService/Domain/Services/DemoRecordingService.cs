using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Services;

public class DemoRecordingService : IDemoRecordingService
{
    private readonly ILogger<DemoRecordingService> _logger;
    private readonly IDemoRecordingRepository _demoRecordingsRepository;

    public DemoRecordingService(ILogger<DemoRecordingService> logger, IDemoRecordingRepository demoRecordingsRepository)
    {
        _logger = logger;
        _demoRecordingsRepository = demoRecordingsRepository;
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
