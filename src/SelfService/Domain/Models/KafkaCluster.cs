namespace SelfService.Domain.Models;

public class KafkaCluster : AggregateRoot<KafkaClusterId>
{
    public KafkaCluster(KafkaClusterId id, string name, string description, bool enabled) : base(id)
    {
        Name = name;
        Description = description;
        Enabled = enabled;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
}