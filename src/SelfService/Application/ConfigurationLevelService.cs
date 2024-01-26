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

    public bool IsFocusMetric{ get; set; }
    public Dictionary<string, ConfigurationLevelDetail> breakdown{ get; set; }
    
    public ConfigurationLevelInfo()
    {
        overallLevel = ConfigurationLevel.None;
        IsFocusMetric = false;
        breakdown = new Dictionary<string, ConfigurationLevelDetail>();
    }

    public void Add(ConfigurationLevelDetail detail)
    {
        breakdown[detail.description] = detail;
    }
    
    public IEnumerable<ConfigurationLevelDetail> GetBreakdown()
    {
        return breakdown.Values;
    }
}

public class ConfigurationLevelService : IConfigurationLevelService
{

    public async Task<ConfigurationLevelInfo> ComputeConfigurationLevel(CapabilityId capabilityId)
    {
        var configLevelInfo = new ConfigurationLevelInfo();
        configLevelInfo.Add(new ConfigurationLevelDetail(
            await GetKafkaTopicConfigurationLevel(), 
            "kafka-topics-schemas-configured", 
            "", 
            false)
        );
        configLevelInfo.Add(new ConfigurationLevelDetail(
            await GetCostCenterTaggingConfigurationLevel(),
            "cost-center-tagging",
            "",
            true)
        );
        configLevelInfo.Add(new ConfigurationLevelDetail(
            await GetSecurityTaggingConfigurationLevel(),
            "security-tagging",
            "",
            false)
        );
        
        int numComplete = configLevelInfo.breakdown.Values.Count(detail => detail.level == ConfigurationLevel.Complete);
        int numPartial  = configLevelInfo.breakdown.Values.Count(detail => detail.level == ConfigurationLevel.Partial);

        if (numPartial > 0 || numComplete > 0)
        {
            configLevelInfo.overallLevel = ConfigurationLevel.Partial;
        }
        if (numComplete == configLevelInfo.breakdown.Values.Count()){
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




