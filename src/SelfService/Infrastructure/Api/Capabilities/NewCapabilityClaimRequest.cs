using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewSelfAssessmentRequest
{
    [Required]
    public string? selfAssessmentType { get; set; } = null;
}
