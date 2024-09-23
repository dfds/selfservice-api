using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentListApiResource
{
    public List<SelfAssessmentsApiResource> SelfAssessments { get; set; }

    [JsonPropertyName("_links")]
    public SelfAssessmentListLinks Links { get; set; }

    public class SelfAssessmentListLinks
    {
        public ResourceLink Self { get; set; }

        public SelfAssessmentListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public SelfAssessmentListApiResource(
        List<SelfAssessmentsApiResource> selfAssessments,
        SelfAssessmentListLinks links
    )
    {
        SelfAssessments = selfAssessments;
        Links = links;
    }
}
