namespace SelfService.Legacy.Models;

public class Topic
{
    public Guid Id { get; set; }
    public string CapabilityId { get; set; }
    public Guid KafkaClusterId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public int Partitions { get; set; }
    public long Retention { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastModified { get; set; }
}