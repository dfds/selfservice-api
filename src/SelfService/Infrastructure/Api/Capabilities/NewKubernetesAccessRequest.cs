using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewKubernetesAccessRequest
{
    [Required]
    public string? AwsAccountId { get; set; } = null;
}
