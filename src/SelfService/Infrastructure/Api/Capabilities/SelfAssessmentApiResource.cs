using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentsApiResource
{
    public string ShortName { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string Description { get; set; }
    public string DocumentationUrl { get; set; }

    [JsonPropertyName("_links")]
    public SelfAssessmentLinks Links { get; set; }

    public class SelfAssessmentLinks
    {
        public ResourceLink? addSelfAssessment { get; set; }
        public ResourceLink? removeSelfAssessment { get; set; }

        public SelfAssessmentLinks(ResourceLink? addSelfAssessment, ResourceLink? removeSelfAssessment)
        {
            this.addSelfAssessment = addSelfAssessment;
            this.removeSelfAssessment = removeSelfAssessment;
        }
    }

    public SelfAssessmentsApiResource(
        string shortName,
        string description,
        string documentationUrl,
        DateTime? assessedAt,
        SelfAssessmentLinks links
    )
    {
        ShortName = shortName;
        Description = description;
        DocumentationUrl = documentationUrl;
        AssessedAt = assessedAt;
        Links = links;
    }
}
