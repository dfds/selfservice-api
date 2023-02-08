namespace SelfService.Infrastructure.Api.Me;

public class MyCapability
{
    public string Id { get; set; }
    public string RootId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Link[] Links { get; set; } = Array.Empty<Link>();
}