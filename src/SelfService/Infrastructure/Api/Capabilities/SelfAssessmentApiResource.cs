using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentsApiResource
{
    public string SelfAssessmentType { get; set; }
    public DateTime? AssessedAt { get; set; }
    public string Description { get; set; }

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
        string selfAssessmentType,
        string description,
        DateTime? assessedAt,
        SelfAssessmentLinks links
    )
    {
        SelfAssessmentType = selfAssessmentType;
        Description = description;
        AssessedAt = assessedAt;
        Links = links;
    }
}
