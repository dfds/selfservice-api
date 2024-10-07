using System;

namespace SelfService.Domain.Models;

public class KafkaSchemaMetadata
{
    public KafkaSchemaMetadata(
        Dictionary<string, List<string>> tags,
        Dictionary<string, string> properties,
        List<string> sensitive
    )
    {
        Tags = tags;
        Properties = properties;
        Sensitive = sensitive;
    }

    public Dictionary<string, List<string>> Tags { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public List<string> Sensitive { get; set; }   
}