namespace SelfService.Domain.Models;

public class SelfAssessmentOption : AggregateRoot<SelfAssessmentOptionId>
{
    public SelfAssessmentOption(
        SelfAssessmentOptionId id,
        string shortName,
        string description,
        string documentationUrl,
        bool isActive,
        DateTime requestedAt,
        string requestedBy
    )
        : base(id)
    {
        ShortName = shortName;
        Description = description;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        DocumentationUrl = documentationUrl;
        IsActive = isActive;
    }

    public string ShortName { get; private set; }
    public string Description { get; private set; }
    public string DocumentationUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }

    public static SelfAssessmentOption New(
        UserId userId,
        string? shortName,
        string? description,
        string? documentationUrl
    )
    {
        var option = new SelfAssessmentOption(
            id: new SelfAssessmentOptionId(Guid.NewGuid()),
            shortName: shortName ?? string.Empty,
            description: description ?? string.Empty,
            documentationUrl: documentationUrl ?? string.Empty,
            isActive: false,
            requestedAt: DateTime.UtcNow,
            requestedBy: userId
        );

        return option;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Update(string? shortName, string? description, string? documentationUrl)
    {
        ShortName = shortName ?? ShortName;
        Description = description ?? Description;
        DocumentationUrl = documentationUrl ?? DocumentationUrl;
    }
}
