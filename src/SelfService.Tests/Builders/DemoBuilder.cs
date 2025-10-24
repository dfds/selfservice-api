using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class DemoBuilder
{
    private DemoId _id;
    private DateTime _recordingDate;
    private string _title;
    private string _description;
    private string _uri;
    private string _tags;
    private DateTime _createdAt;
    private string _createdBy;
    private bool _isActive;

    public DemoBuilder()
    {
        _id = new DemoId();
        _title = "Default Title";
        _description = "Default Description";
        _uri = "http://default.uri";
        _tags = "Default Tags";
        _recordingDate = DateTime.Now;
        _createdAt = DateTime.Now;
        _createdBy = "DefaultUser";
        _isActive = true;
    }

    public DemoBuilder WithId(DemoId id)
    {
        _id = id;
        return this;
    }

    public DemoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public DemoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public DemoBuilder WithUri(string uri)
    {
        _uri = uri;
        return this;
    }

    public DemoBuilder WithTags(string tags)
    {
        _tags = tags;
        return this;
    }

    public DemoBuilder WithRecordingDate(DateTime recordingDate)
    {
        _recordingDate = recordingDate;
        return this;
    }

    public DemoBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public Demo Build()
    {
        var demo = new Demo(
            id: _id,
            title: _title,
            recordingDate: _recordingDate,
            description: _description,
            uri: _uri,
            tags: _tags,
            createdAt: _createdAt,
            createdBy: _createdBy,
            isActive: _isActive
        );

        return demo;
    }

    public static implicit operator Demo(DemoBuilder builder) => builder.Build();
}
