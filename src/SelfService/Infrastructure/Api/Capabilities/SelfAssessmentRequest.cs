using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SelfAssessmentRequest
{
    [Required]
    public string? SelfAssessmentOptionId { get; set; }

    [Required]
    public string? SelfAssessmentStatus { get; set; }
}
