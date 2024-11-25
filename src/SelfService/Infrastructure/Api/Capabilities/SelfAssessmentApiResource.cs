using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentsApiResource
{
    public string ShortName { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string Description { get; set; }
    public string DocumentationUrl { get; set; }
    public string? Status { get; set; }
    public string[] StatusOptions { get; set; } = SelfAssessmentStatus.Values.Select(x => x.ToString()).ToArray();

    [JsonPropertyName("_links")]
    public SelfAssessmentLinks Links { get; set; }

    public class SelfAssessmentLinks
    {
        public ResourceLink? updateSelfAssessment { get; set; }

        public SelfAssessmentLinks(ResourceLink updateSelfAssessment)
        {
            this.updateSelfAssessment = updateSelfAssessment;
        }
    }

    public SelfAssessmentsApiResource(
        string shortName,
        string description,
        string documentationUrl,
        string? status,
        DateTime? assessedAt,
        SelfAssessmentLinks links
    )
    {
        ShortName = shortName;
        Description = description;
        DocumentationUrl = documentationUrl;
        Status = status;
        AssessedAt = assessedAt;
        Links = links;
    }
}
