namespace SelfService.Domain;

public class ServiceDescription
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Spec { get; set; }
    public DateTime CreatedAt { get; set; }
}
