namespace SelfService.Domain.Models;

public class ServiceDescription
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Spec { get; set; }
    public DateTime CreatedAt { get; set; }

    public ServiceDescription(Guid id, string name, string @namespace, string spec, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Namespace = @namespace;
        Spec = spec;
        CreatedAt = createdAt;
    }
}
