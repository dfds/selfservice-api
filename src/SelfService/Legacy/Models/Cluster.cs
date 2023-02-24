#pragma warning disable CS8618
namespace SelfService.Legacy.Models;

public class Cluster
{
    public Guid Id { get; set; }
    public string? ClusterId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Enabled { get; set; }
}