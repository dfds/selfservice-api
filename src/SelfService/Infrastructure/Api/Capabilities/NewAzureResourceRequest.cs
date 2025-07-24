using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewAzureResourceRequest
{
    [Required]
    public string? environment { get; set; } = null;

    [Required]
    public string? purpose { get; set; } = null;
    public string? catalogueId { get; set; } = null;
    public string? risk { get; set; } = null;
    public bool? gdpr { get; set; } = null;
}
