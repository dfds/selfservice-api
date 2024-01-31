using SelfService.Domain.Models;
using System.Text.Json.Nodes;

namespace SelfService.Application;

public enum ConfigurationLevel
{
    None,
    Partial,
    Complete
}

public class ConfigurationLevelDetail : Entity<ConfigurationLevelDetail>
{
    public ConfigurationLevel level { get; set; }
    public string identifier { get; set; }
    public string description { get; set; }
    public string suggestion { get; set; }
    public bool isFocusMetric { get; set; }

    public ConfigurationLevelDetail(
        ConfigurationLevel level,
        string identifier,
        string description,
        string suggestion,
        bool isFocusMetric
    )
    {
        this.level = level;
        this.identifier = identifier;
        this.description = description;
        this.suggestion = suggestion;
        this.isFocusMetric = isFocusMetric;
    }
}

public class ConfigurationLevelInfo : Entity<ConfigurationLevelInfo>
{
    public ConfigurationLevel overallLevel { get; set; }
    public List<ConfigurationLevelDetail> breakdown { get; set; }

    public ConfigurationLevelInfo()
    {
        overallLevel = ConfigurationLevel.None;
        breakdown = new List<ConfigurationLevelDetail>();
    }

    public void AddMetric(ConfigurationLevelDetail detail)
    {
        var existingDetail = breakdown.FirstOrDefault(d => d.description == detail.description);
        if (existingDetail != null)
        {
            breakdown[breakdown.IndexOf(existingDetail)] = detail;
        }
        else
        {
            breakdown.Add(detail);
        }
    }
}

public class ConfigurationLevelService : IConfigurationLevelService
{
    private readonly ICapabilityRepository _capabilityRepository;

    public ConfigurationLevelService(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
    }

    public async Task<ConfigurationLevelInfo> ComputeConfigurationLevel(CapabilityId capabilityId)
    {
        var configLevelInfo = new ConfigurationLevelInfo();
        configLevelInfo.AddMetric(
            new ConfigurationLevelDetail(
                await GetKafkaTopicConfigurationLevel(),
                "kafka-topics-schemas-configured",
                "Document Kafka topics.",
                "Make sure all public Kafka topics for this capability have schemas connected to them.",
                false
            )
        );
        configLevelInfo.AddMetric(
            new ConfigurationLevelDetail(
                await GetCostCenterTaggingConfigurationLevel(_capabilityRepository, capabilityId),
                "cost-centre-tagging",
                "Cost Centre known.",
                "Update the Cost Centre tag for this capability to match your team's Cost Centre.",
                true
            )
        );
        configLevelInfo.AddMetric(
            new ConfigurationLevelDetail(
                await GetSecurityTaggingConfigurationLevel(),
                "security-tagging",
                "Criticality level understood.",
                "Make sure all optional security tags are set to a correct value for this capability.",
                false
            )
        );

        int numComplete = configLevelInfo.breakdown.Count(detail => detail.level == ConfigurationLevel.Complete);
        int numPartial = configLevelInfo.breakdown.Count(detail => detail.level == ConfigurationLevel.Partial);

        if (numPartial > 0 || numComplete > 0)
        {
            configLevelInfo.overallLevel = ConfigurationLevel.Partial;
        }
        if (numComplete == configLevelInfo.breakdown.Count())
        {
            configLevelInfo.overallLevel = ConfigurationLevel.Complete;
        }

        return configLevelInfo;
    }

    public async Task<ConfigurationLevel> GetKafkaTopicConfigurationLevel()
    {
        //TODO: implement
        await Task.CompletedTask;
        return ConfigurationLevel.Partial;
    }

    public async Task<ConfigurationLevel> GetCostCenterTaggingConfigurationLevel(
        ICapabilityRepository capabilityRepository,
        CapabilityId capabilityId
    )
    {
        var jsonString = await capabilityRepository.GetJsonMetadata(capabilityId);
        if (jsonString == null)
        {
            return ConfigurationLevel.None;
        }
        var jsonObject = JsonNode.Parse(jsonString)?.AsObject()!;

        var costCenter = jsonObject["dfds.cost.centre"];
        if (costCenter == null || costCenter.ToString() == "")
        {
            return ConfigurationLevel.None;
        }
        return ConfigurationLevel.Complete;
    }

    public async Task<ConfigurationLevel> GetSecurityTaggingConfigurationLevel()
    {
        //TODO: implement
        await Task.CompletedTask;
        return ConfigurationLevel.Partial;
    }
}
