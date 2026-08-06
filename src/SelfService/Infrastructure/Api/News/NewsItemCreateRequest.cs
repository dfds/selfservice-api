using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.News;

public class NewsItemCreateRequest
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Body { get; set; }

    public string? FrontpageSummary { get; set; }

    [Required]
    public DateTime DueDate { get; set; }
}
