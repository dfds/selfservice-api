using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Models;

public class Demo : Entity<DemoId>
{
    public DateTime RecordingDate { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Uri { get; private set; }
    public string Tags { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public Demo(
        DemoId id,
        DateTime recordingDate,
        string title,
        string description,
        string uri,
        string tags,
        string createdBy,
        DateTime createdAt,
        bool isActive = true
    )
        : base(id)
    {
        RecordingDate = recordingDate;
        Title = title;
        Description = description;
        Uri = uri;
        Tags = tags;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IsActive = isActive;
    }

    public void Update(DemoUpdateRequest demoDataRequest)
    {
        if (demoDataRequest == null)
        {
            throw new ArgumentNullException(nameof(demoDataRequest));
        }

        Title = demoDataRequest.Title ?? Title;
        Description = demoDataRequest.Description ?? Description;
        Uri = demoDataRequest.Uri ?? Uri;
        Tags = demoDataRequest.Tags ?? Tags;
        RecordingDate = demoDataRequest.RecordingDate;
        IsActive = demoDataRequest.IsActive;
    }
}
