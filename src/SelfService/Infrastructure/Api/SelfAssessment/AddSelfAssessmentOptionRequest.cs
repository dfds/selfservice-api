using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class AddSelfAssessmentOptionRequest
{
    [Required]
    public string ShortName { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string DocumentationUrl { get; set; } = string.Empty;
}
