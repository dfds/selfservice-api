namespace SelfService.Infrastructure.Api.News;

public class NewsItemUpdateRequest
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? FrontpageSummary { get; set; }
    public List<Guid>? AssetIds { get; set; }
    public DateTime? DueDate { get; set; }
}
