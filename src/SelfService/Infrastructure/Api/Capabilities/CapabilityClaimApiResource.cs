using System.Text.Json.Serialization;
using Confluent.Kafka;

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
        public ResourceLink? SelfAssessment { get; set; }

        public SelfAssessmentLinks(ResourceLink? selfAssessment)
        {
            SelfAssessment = selfAssessment;
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
