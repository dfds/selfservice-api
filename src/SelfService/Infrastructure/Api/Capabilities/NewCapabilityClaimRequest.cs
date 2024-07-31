using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewCapabilityClaimRequest
{
    [Required]
    public string? claim { get; set; } = null;
}
