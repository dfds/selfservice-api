using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Services;

public interface IDemoRecordingService
{
    Task<IEnumerable<DemoRecording>> GetAllDemoRecordings();
    Task<DemoRecording> GetDemoRecordingById(DemoRecordingId demoId);
    Task<DemoRecording> AddDemoRecording(DemoRecording createRequest);
    Task UpdateDemoRecording(DemoRecordingId demoId, DemoRecordingUpdateRequest updateRequest);
    Task DeleteDemoRecording(DemoRecordingId demoId);
}
