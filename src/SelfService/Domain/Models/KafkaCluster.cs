namespace SelfService.Domain.Models;

public class KafkaCluster : AggregateRoot<KafkaClusterId>
{
    public KafkaCluster(KafkaClusterId id, string realClusterId, string name, string description, bool enabled) : base(id)
    {
        RealClusterId = realClusterId;
        Name = name;
        Description = description;
        Enabled = enabled;
    }

    public string RealClusterId { get; private set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
}