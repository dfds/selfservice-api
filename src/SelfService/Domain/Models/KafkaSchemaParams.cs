using System;

namespace SelfService.Domain.Models;

public class KafkaSchemaParams
{
    public KafkaSchemaParams(string property1, string property2)
    {
        Property1 = property1;
        Property2 = property2;
    }

    public string Property1 { get; set; }
    public string Property2 { get; set; }
}
