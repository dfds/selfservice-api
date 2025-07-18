using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class CapabilityApplicationServiceBuilder
{
    private ICapabilityRepository _capabilityRepository;
    private ISelfServiceJsonSchemaService _selfServiceJsonSchemaService;
    private IConfigurationLevelService _configurationLevelService;

    public CapabilityApplicationServiceBuilder()
    {
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
        _selfServiceJsonSchemaService = Dummy.Of<ISelfServiceJsonSchemaService>();
        _configurationLevelService = Dummy.Of<IConfigurationLevelService>();
    }

    public CapabilityApplicationServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public CapabilityApplicationServiceBuilder WithSelfServiceJsonSchemaService(
        ISelfServiceJsonSchemaService selfServiceJsonSchemaService
    )
    {
        _selfServiceJsonSchemaService = selfServiceJsonSchemaService;
        return this;
    }

    public CapabilityApplicationServiceBuilder WithConfigurationLevelService(
        IConfigurationLevelService configurationLevelService
    )
    {
        _configurationLevelService = configurationLevelService;
        return this;
    }

    public CapabilityApplicationService Build()
    {
        return new CapabilityApplicationService(
            logger: NullLogger<CapabilityApplicationService>.Instance,
            capabilityRepository: _capabilityRepository,
            kafkaTopicRepository: Mock.Of<IKafkaTopicRepository>(),
            selfAssessmentRepository: Mock.Of<ISelfAssessmentRepository>(),
            selfAssessmentOptionRepository: Mock.Of<ISelfAssessmentOptionRepository>(),
            kafkaClusterAccessRepository: Mock.Of<IKafkaClusterAccessRepository>(),
            membershipRepository: Mock.Of<IMembershipRepository>(),
            ticketingSystem: Mock.Of<ITicketingSystem>(),
            systemTime: SystemTime.Default,
            selfServiceJsonSchemaService: _selfServiceJsonSchemaService,
            configurationLevelService: _configurationLevelService,
            confluentGatewayService: Mock.Of<IConfluentGatewayService>()
        );
    }

    public static implicit operator CapabilityApplicationService(CapabilityApplicationServiceBuilder builder) =>
        builder.Build();
}
