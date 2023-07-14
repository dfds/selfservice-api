using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class DeactivatedMemberCleanerApplicationServiceBuilder
{
    public NullLogger<SelfService.Application.DeactivatedMemberCleanerApplicationService> logger;
    private SystemTime _systemTime;
    // add fields we need, logger, dbContext, userStatusChecker, etc

    public DeactivatedMemberCleanerApplicationServiceBuilder()
    {
        _systemTime = SystemTime.Default;
        //_kafkaTopicRepository = Dummy.Of<IKafkaTopicRepository>();
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithMessageContractRepository(IMessageContractRepository messageContractRepository)
    {
        //_messageContractRepository = messageContractRepository;
        return this;
    }

    public DeactivatedMemberCleanerApplicationServiceBuilder WithSystemTime(DateTime systemTime)
    {
        _systemTime = new SystemTime(() => systemTime);
        return this;
    }


    public SelfService.Application.DeactivatedMemberCleanerApplicationService Build()
    {
        return new SelfService.Application.DeactivatedMemberCleanerApplicationService(
            logger: NullLogger<SelfService.Application.DeactivatedMemberCleanerApplicationService>.Instance,
            systemTime: _systemTime
        );
    }

    public static implicit operator SelfService.Application.DeactivatedMemberCleanerApplicationService(DeactivatedMemberCleanerApplicationServiceBuilder builder)
       => builder.Build();
}