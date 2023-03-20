namespace SelfService.Infrastructure.Api.Capabilities;

public class ResourceLink
{
    public string Href { get; set; }
    public string Rel { get; set; }
    public List<string> Allow { get; set; } = new();
}