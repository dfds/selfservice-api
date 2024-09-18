using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class ConfigurationLevelServiceBuilder
{
    private ICapabilityRepository _capabilityRepository = Dummy.Of<ICapabilityRepository>();
    private ICapabilityClaimRepository _capabilityClaimRepository = Dummy.Of<ICapabilityClaimRepository>();
    private readonly IKafkaTopicRepository _kafkaTopicRepository = Dummy.Of<IKafkaTopicRepository>();
    private readonly IMessageContractRepository _messageContractRepository = Dummy.Of<IMessageContractRepository>();

    public ConfigurationLevelServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;

        return this;
    }

    public ConfigurationLevelServiceBuilder WithCapabilityClaimRepository(
        ICapabilityClaimRepository capabilityClaimRepository
    )
    {
        _capabilityClaimRepository = capabilityClaimRepository;

        return this;
    }

    public ConfigurationLevelService Build()
    {
        return new ConfigurationLevelService(
            _kafkaTopicRepository,
            _messageContractRepository,
            _capabilityRepository,
            _capabilityClaimRepository
        );
    }

    public static implicit operator ConfigurationLevelService(ConfigurationLevelServiceBuilder builder) =>
        builder.Build();
}
