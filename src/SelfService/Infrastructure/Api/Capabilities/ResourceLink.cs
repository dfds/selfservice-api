namespace SelfService.Infrastructure.Api.Capabilities;

public class ResourceLink
{
    public string Href { get; set; }
    public string Rel { get; set; }
    public Allow Allow { get; set; } = new();

    //public ResourceLink() { }
    public ResourceLink(string href, string rel, Allow allow)
    {
        Href = href;
        Rel = rel;
        Allow = allow;
    }
}

public class ResourceActionLink
{
    public string Href { get; set; }
    public string Method { get; set; }

    public ResourceActionLink(string href, string method)
    {
        Href = href;
        Method = method;
    }
}
