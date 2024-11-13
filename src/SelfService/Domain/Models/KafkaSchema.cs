using System;

namespace SelfService.Domain.Models;

public class KafkaSchema
{
    public KafkaSchema(
        string subject,
        int version,
        int id,
        string schemaType,
        List<KafkaSchemaReference> references,
        string schema,
        KafkaSchemaMetadata metadata,
        KafkaSchemaRuleSet ruleSet
    )
    {
        Subject = subject;
        Version = version;
        ID = id;
        SchemaType = schemaType;
        References = references;
        Schema = schema;
        Metadata = metadata;
        RuleSet = ruleSet;
    }

    public string Subject { get; set; }
    public int Version { get; set; }
    public int ID { get; set; }
    public string SchemaType { get; set; }
    public List<KafkaSchemaReference> References { get; set; }
    public string Schema { get; set; }
    public KafkaSchemaMetadata Metadata { get; set; }
    public KafkaSchemaRuleSet RuleSet { get; set; }
}
