namespace SelfService.Infrastructure.Api.Capabilities;

public class ResourceLink
{
    public string Href { get; set; }
    public string Rel { get; set; }
    public Allow Allow { get; set; } = new();
}

public class ResourceActionLink
{
    public string Href { get; set; }
    public string Method { get; set; }
}
