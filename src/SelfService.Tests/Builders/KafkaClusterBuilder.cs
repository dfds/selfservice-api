using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class KafkaClusterBuilder
{
    private KafkaClusterId _id;
    private string _name;
    private string _description;
    private bool _enabled;

    public KafkaClusterBuilder()
    {
        _id = KafkaClusterId.Parse("cluster foo");
        _name = "bar";
        _description = "baz";
        _enabled = true;
    }

    public KafkaCluster Build()
    {
        return new KafkaCluster(
            id: _id,
            name: _name,
            description: _description,
            enabled: _enabled
        );
    }

    public static implicit operator KafkaCluster(KafkaClusterBuilder builder)
        => builder.Build();
}