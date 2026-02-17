using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Models;

public class DemoRecording : Entity<DemoRecordingId>
{
    public DateTime RecordingDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string RecordingUrl { get; private set; }
    public string SlidesUrl { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public DemoRecording(
        DemoRecordingId id,
        DateTime recordingDate,
        string title,
        string description,
        string recordingUrl,
        string slidesUrl,
        string createdBy,
        DateTime createdAt
    )
        : base(id)
    {
        RecordingDate = recordingDate;
        Title = title;
        Description = description;
        RecordingUrl = recordingUrl;
        SlidesUrl = slidesUrl;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public void Update(DemoRecordingUpdateRequest demoDataRequest)
    {
        if (demoDataRequest == null)
        {
            throw new ArgumentNullException(nameof(demoDataRequest));
        }

        Title = demoDataRequest.Title ?? Title;
        Description = demoDataRequest.Description ?? Description;
        RecordingUrl = demoDataRequest.RecordingUrl ?? RecordingUrl;
        SlidesUrl = demoDataRequest.SlidesUrl ?? SlidesUrl;
        RecordingDate = demoDataRequest.RecordingDate;
    }
}
