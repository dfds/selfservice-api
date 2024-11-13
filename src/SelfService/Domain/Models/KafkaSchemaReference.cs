using System;

namespace SelfService.Domain.Models;

public class KafkaSchemaReference
{
    public KafkaSchemaReference(string name, string subject, int version)
    {
        Name = name;
        Subject = subject;
        Version = version;
    }

    public string Name { get; set; }
    public string Subject { get; set; }
    public int Version { get; set; }
}
