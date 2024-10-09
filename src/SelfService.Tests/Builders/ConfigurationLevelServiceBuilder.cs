using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ConfigurationLevelServiceBuilder
{
    private ICapabilityRepository _capabilityRepository = Dummy.Of<ICapabilityRepository>();
    private ISelfAssessmentRepository _selfAssessmentRepository = Dummy.Of<ISelfAssessmentRepository>();
    private ISelfAssessmentOptionRepository _selfAssessmentOptionRepository =
        Dummy.Of<ISelfAssessmentOptionRepository>();
    private readonly IKafkaTopicRepository _kafkaTopicRepository = Dummy.Of<IKafkaTopicRepository>();
    private readonly IMessageContractRepository _messageContractRepository = Dummy.Of<IMessageContractRepository>();

    public ConfigurationLevelServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;

        return this;
    }

    public ConfigurationLevelServiceBuilder WithSelfAssessmentRepository(
        ISelfAssessmentRepository selfAssessmentRepository
    )
    {
        _selfAssessmentRepository = selfAssessmentRepository;

        return this;
    }

    public ConfigurationLevelService Build()
    {
        return new ConfigurationLevelService(
            _kafkaTopicRepository,
            _messageContractRepository,
            _capabilityRepository,
            _selfAssessmentRepository,
            _selfAssessmentOptionRepository
        );
    }

    public static implicit operator ConfigurationLevelService(ConfigurationLevelServiceBuilder builder) =>
        builder.Build();
}
