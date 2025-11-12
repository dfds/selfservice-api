using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class DemoRecordingBuilder
{
    private DemoRecordingId _id;
    private DateTime _recordingDate;
    private string _title;
    private string _description;
    private string _url;
    private DateTime _createdAt;
    private string _createdBy;
    
    public DemoRecordingBuilder()
    {
        _id = new DemoRecordingId();
        _title = "Default Title";
        _description = "Default Description";
        _url = "http://default.uri";
        _recordingDate = DateTime.Now;
        _createdAt = DateTime.Now;
        _createdBy = "DefaultUser";
    }

    public DemoRecordingBuilder WithId(DemoRecordingId id)
    {
        _id = id;
        return this;
    }

    public DemoRecordingBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public DemoRecordingBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public DemoRecordingBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    public DemoRecordingBuilder WithRecordingDate(DateTime recordingDate)
    {
        _recordingDate = recordingDate;
        return this;
    }

    public DemoRecording Build()
    {
        return new DemoRecording(
            id: _id,
            title: _title,
            recordingDate: _recordingDate,
            description: _description,
            url: _url,
            createdAt: _createdAt,
            createdBy: _createdBy
        );
    }

    public static implicit operator DemoRecording(DemoRecordingBuilder builder) => builder.Build();
}
