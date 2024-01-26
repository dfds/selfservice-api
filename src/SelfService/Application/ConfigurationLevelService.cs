using SelfService.Domain.Models;
using System.Linq;

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
    public string description { get; set; }
    public string suggestion { get; set; }
    public bool isFocusMetric { get; set; }

    public ConfigurationLevelDetail(ConfigurationLevel level, string description, string suggestion, bool isFocusMetric)
    {
        this.level = level;
        this.description = description;
        this.suggestion = suggestion;
        this.isFocusMetric = isFocusMetric;
    }
}

public class ConfigurationLevelInfo : Entity<ConfigurationLevelInfo>
{
    public ConfigurationLevel overallLevel { get; set; }
    public bool IsFocusMetric { get; set; }
    public List<ConfigurationLevelDetail> breakdown { get; set; }

    public ConfigurationLevelInfo()
    {
        overallLevel = ConfigurationLevel.None;
        IsFocusMetric = false;
        breakdown = new List<ConfigurationLevelDetail>();
    }

    public void Add(ConfigurationLevelDetail detail)
    {
        var existingDetail = breakdown.FirstOrDefault(d => d.description == detail.description);
        if (existingDetail != null)
        {
            // Replace the existing item
            breakdown[breakdown.IndexOf(existingDetail)] = detail;
        }
        else
        {
            // Add a new item
            breakdown.Add(detail);
        }
    }
}

public class ConfigurationLevelService : IConfigurationLevelService
{
    public async Task<ConfigurationLevelInfo> ComputeConfigurationLevel(CapabilityId capabilityId)
    {
        var configLevelInfo = new ConfigurationLevelInfo();
        configLevelInfo.Add(
            new ConfigurationLevelDetail(
                await GetKafkaTopicConfigurationLevel(),
                "kafka-topics-schemas-configured",
                "",
                false
            )
        );
        configLevelInfo.Add(
            new ConfigurationLevelDetail(
                await GetCostCenterTaggingConfigurationLevel(),
                "cost-center-tagging",
                "",
                true
            )
        );
        configLevelInfo.Add(
            new ConfigurationLevelDetail(await GetSecurityTaggingConfigurationLevel(), "security-tagging", "", false)
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

    public async Task<ConfigurationLevel> GetCostCenterTaggingConfigurationLevel()
    {
        //TODO: implement
        await Task.CompletedTask;
        return ConfigurationLevel.Partial;
    }

    public async Task<ConfigurationLevel> GetSecurityTaggingConfigurationLevel()
    {
        //TODO: implement
        await Task.CompletedTask;
        return ConfigurationLevel.Partial;
    }
}
