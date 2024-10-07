using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class UpdateSelfAssessmentOptionRequest
{
    public string ShortName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DocumentationUrl { get; set; } = string.Empty;
}
