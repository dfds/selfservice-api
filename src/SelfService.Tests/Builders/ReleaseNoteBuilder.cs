using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class ReleaseNoteBuilder
{
    private ReleaseNoteId _id;
    private string _title;
    private string _content;
    private DateTime _releaseDate;
    private DateTime _createdAt;
    private DateTime _modifiedAt;
    private string _createdBy;
    private string _modifiedBy;
    private bool _isActive;

    public ReleaseNoteBuilder()
    {
        _id = ReleaseNoteId.New();
        _title = "Default Title";
        _content = "Default Content";
        _releaseDate = DateTime.Now;
        _createdAt = DateTime.Now;
        _modifiedAt = DateTime.Now;
        _createdBy = "DefaultUser";
        _modifiedBy = "DefaultUser";
        _isActive = true;
    }

    public ReleaseNoteBuilder WithId(ReleaseNoteId id)
    {
        _id = id;
        return this;
    }

    public ReleaseNoteBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public ReleaseNoteBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public ReleaseNoteBuilder WithReleaseDate(DateTime releaseDate)
    {
        _releaseDate = releaseDate;
        return this;
    }

    public ReleaseNoteBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public ReleaseNote Build()
    {
        var releaseNote = new ReleaseNote(
            id: _id,
            title: _title,
            releaseDate: _releaseDate,
            content: _content,
            createdAt: _createdAt,
            createdBy: _createdBy,
            modifiedAt: _modifiedAt,
            modifiedBy: _modifiedBy,
            isActive: _isActive
        );

        return releaseNote;
    }

    public static implicit operator ReleaseNote(ReleaseNoteBuilder builder) => builder.Build();
}
