using System;

namespace SelfService.Domain.Models;

public class KafkaSchemaRuleSet
{
    public KafkaSchemaRuleSet(List<KafkaSchemaRule> migrationRules, List<KafkaSchemaRule> domainRules)
    {
        MigrationRules = migrationRules;
        DomainRules = domainRules;
    }

    public List<KafkaSchemaRule> MigrationRules { get; set; }
    public List<KafkaSchemaRule> DomainRules { get; set; }
}
