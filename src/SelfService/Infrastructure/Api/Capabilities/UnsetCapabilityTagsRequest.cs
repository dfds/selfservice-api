using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class UnsetCapabilityTagsRequest
{
    [Required]
    public List<string>? Tags { get; set; }
}
