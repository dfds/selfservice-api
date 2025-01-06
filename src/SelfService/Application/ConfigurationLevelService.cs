using System.Linq;
using System.Text.Json.Nodes;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public enum ConfigurationLevel
{
    None,
    Partial,
    Complete,
    Unknown,
}

public class ConfigurationLevelDetail : Entity<ConfigurationLevelDetail>
{
    public ConfigurationLevel level { get; set; }
    public string identifier { get; set; }
    public string description { get; set; }
    public bool isFocusMetric { get; set; }
    public bool isSelfAssessed { get; set; }

    public ConfigurationLevelDetail(
        ConfigurationLevel level,
        string identifier,
        string description,
        bool isFocusMetric,
        bool isSelfAssessed
    )
    {
        this.level = level;
        this.identifier = identifier;
        this.description = description;
        this.isFocusMetric = isFocusMetric;
        this.isSelfAssessed = isSelfAssessed;
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

    public void AddMetrics(List<ConfigurationLevelDetail> metrics)
    {
        foreach (var metric in metrics)
        {
            AddMetric(metric);
        }
    }
}

public class ConfigurationLevelService : IConfigurationLevelService
{
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly ISelfAssessmentRepository _selfAssessmentRepository;
    private readonly ISelfAssessmentOptionRepository _selfAssessmentOptionRepository;

    public ConfigurationLevelService(
        IKafkaTopicRepository kafkaTopicRepository,
        IMessageContractRepository messageContractRepository,
        ICapabilityRepository capabilityRepository,
        ISelfAssessmentRepository selfAssessmentRepository,
        ISelfAssessmentOptionRepository selfAssessmentOptionRepository
    )
    {
        _kafkaTopicRepository = kafkaTopicRepository;
        _messageContractRepository = messageContractRepository;
        _capabilityRepository = capabilityRepository;
        _selfAssessmentRepository = selfAssessmentRepository;
        _selfAssessmentOptionRepository = selfAssessmentOptionRepository;
    }

    public async Task<ConfigurationLevelInfo> ComputeConfigurationLevel(CapabilityId capabilityId)
    {
        var configLevelInfo = new ConfigurationLevelInfo();
        configLevelInfo.AddMetric(await GetKafkaTopicConfigurationLevel(capabilityId));
        configLevelInfo.AddMetric(await GetCostCenterTaggingConfigurationLevel(capabilityId));
        configLevelInfo.AddMetric(await GetSecurityTaggingConfigurationLevel(capabilityId));
        configLevelInfo.AddMetrics(await GetSelfAssessmentMetrics(capabilityId));

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

    public async Task<ConfigurationLevelDetail> GetKafkaTopicConfigurationLevel(CapabilityId capabilityId)
    {
        var topics = await _kafkaTopicRepository.FindBy(capabilityId);
        var publicTopics = topics.Where(t => t.Name.ToString().StartsWith("pub."));

        int numTopicsWithSchema = 0;
        var totalTopics = publicTopics.Count();
        foreach (KafkaTopic t in publicTopics)
        {
            var schemas = await _messageContractRepository.FindBy(t.Id);
            if (schemas.Any())
            {
                numTopicsWithSchema += 1;
            }
        }

        var configurationLevel = ConfigurationLevel.None;
        if (numTopicsWithSchema == totalTopics)
        {
            configurationLevel = ConfigurationLevel.Complete;
        }
        else if (numTopicsWithSchema > 0)
        {
            configurationLevel = ConfigurationLevel.Complete;
        }

        return new ConfigurationLevelDetail(
            configurationLevel,
            "kafka-topics-schemas-configured",
            $"{numTopicsWithSchema} of {totalTopics} public Kafka topics have schemas",
            false,
            false
        );
    }

    public async Task<ConfigurationLevelDetail> GetCostCenterTaggingConfigurationLevel(CapabilityId capabilityId)
    {
        (ConfigurationLevel configurationLevel, List<string> _) = await MetadataContainsTags(
            capabilityId,
            new List<string> { "dfds.cost.centre" }
        );
        var description = configurationLevel switch
        {
            ConfigurationLevel.None => "Cost Centre unknown",
            ConfigurationLevel.Complete => "Cost Centre known",
            _ => "Cost Centre unknown",
        };

        return new ConfigurationLevelDetail(configurationLevel, "cost-centre-tagging", description, true, false);
    }

    public async Task<ConfigurationLevelDetail> GetSecurityTaggingConfigurationLevel(CapabilityId capabilityId)
    {
        var tagsList = new List<string>
        {
            "dfds.data.classification",
            "dfds.service.availability",
            "dfds.service.criticality",
        };
        (ConfigurationLevel configurationLevel, List<string> missingTags) = await MetadataContainsTags(
            capabilityId,
            tagsList
        );

        return new ConfigurationLevelDetail(
            configurationLevel,
            "security-tagging",
            $"{tagsList.Count - missingTags.Count} of {tagsList.Count} Capability Classification tags set",
            false,
            false
        );
    }

    private ConfigurationLevel translateStatus(SelfAssessmentStatus status)
    {
        if (status == SelfAssessmentStatus.NotApplicable)
        {
            return ConfigurationLevel.Partial;
        }
        if (status == SelfAssessmentStatus.Satisfied)
        {
            return ConfigurationLevel.Complete;
        }
        if (status == SelfAssessmentStatus.Violated)
        {
            return ConfigurationLevel.None;
        }
        return ConfigurationLevel.Unknown;
    }

    public async Task<List<ConfigurationLevelDetail>> GetSelfAssessmentMetrics(CapabilityId capabilityId)
    {
        var metrics = new List<ConfigurationLevelDetail> { };

        var selfAssessmentOptions = await _selfAssessmentOptionRepository.GetActiveSelfAssessmentOptions();
        var assessments = await _selfAssessmentRepository.GetSelfAssessmentsForCapability(capabilityId);

        foreach (SelfAssessmentOption option in selfAssessmentOptions)
        {
            var exists = assessments.Any(a => a.OptionId == option.Id);
            if (!exists)
            {
                metrics.Add(
                    new ConfigurationLevelDetail(
                        ConfigurationLevel.Unknown,
                        option.ShortName,
                        option.Description,
                        false,
                        true
                    )
                );
                continue;
            }
        }
        foreach (SelfAssessment assessment in assessments)
        {
            var option = selfAssessmentOptions.First(o => o.Id == assessment.OptionId);
            metrics.Add(
                new ConfigurationLevelDetail(
                    translateStatus(assessment.Status),
                    option.ShortName,
                    option.Description,
                    false,
                    true
                )
            );
        }

        return metrics;
    }

    private async Task<(ConfigurationLevel, List<string>)> MetadataContainsTags(
        CapabilityId capabilityId,
        List<string> tags
    )
    {
        var jsonString = await _capabilityRepository.GetJsonMetadata(capabilityId);
        if (jsonString == null)
        {
            return (ConfigurationLevel.None, tags);
        }
        var jsonObject = JsonNode.Parse(jsonString)?.AsObject()!;

        var missingTags = tags.Where(tag => jsonObject[tag] == null || jsonObject[tag]?.ToString() == "").ToList();

        if (missingTags.Count == 0)
        {
            return (ConfigurationLevel.Complete, new List<string>());
        }
        if (missingTags.Count == tags.Count)
        {
            return (ConfigurationLevel.None, tags);
        }
        return (ConfigurationLevel.Partial, missingTags);
    }
}
