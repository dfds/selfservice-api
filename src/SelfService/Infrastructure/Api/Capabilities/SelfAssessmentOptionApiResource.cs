using System.Text.Json.Serialization;
using Confluent.Kafka;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentOptionApiResource
{
    public string Id { get; set; }
    public string ShortName { get; set; }
    public string Description { get; set; }
    public string DocumentationUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; set; }

    [JsonPropertyName("_links")]
    public SelfAssessmentOptionLinks Links { get; set; }

    public class SelfAssessmentOptionLinks
    {
        public ResourceLink? SelfAssessmentOption { get; set; }

        public SelfAssessmentOptionLinks(ResourceLink? selfAssessmentOption)
        {
            SelfAssessmentOption = selfAssessmentOption;
        }
    }

    public SelfAssessmentOptionApiResource(
        SelfAssessmentOptionId id,
        string shortName,
        string description,
        string documentationUrl,
        bool isActive,
        DateTime requestedAt,
        string requestedBy,
        SelfAssessmentOptionLinks links
    )
    {
        Id = id.ToString();
        ShortName = shortName;
        Description = description;
        DocumentationUrl = documentationUrl;
        IsActive = isActive;
        RequestedAt = requestedAt;
        RequestedBy = requestedBy;
        Links = links;
    }
}
