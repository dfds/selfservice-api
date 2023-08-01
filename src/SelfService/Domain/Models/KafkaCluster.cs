using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SelfService.Domain.Models;

public class KafkaCluster : AggregateRoot<KafkaClusterId>
{
    public KafkaCluster(
        KafkaClusterId id,
        string name,
        string description,
        bool enabled,
        string bootstrapServers,
        string schemaRegistryUrl
    )
        : base(id)
    {
        Name = name;
        Description = description;
        Enabled = enabled;
        BootstrapServers = bootstrapServers;
        SchemaRegistryUrl = schemaRegistryUrl;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; }
    public string SchemaRegistryUrl { get; set; }
}
