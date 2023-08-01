using Microsoft.Extensions.Logging.Abstractions;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class KafkaTopicApplicationServiceBuilder
{
    private IMessageContractRepository _messageContractRepository;
    private SystemTime _systemTime;
    private IKafkaTopicRepository _kafkaTopicRepository;

    public KafkaTopicApplicationServiceBuilder()
    {
        _messageContractRepository = Dummy.Of<IMessageContractRepository>();
        _systemTime = SystemTime.Default;
        _kafkaTopicRepository = Dummy.Of<IKafkaTopicRepository>();
    }

    public KafkaTopicApplicationServiceBuilder WithMessageContractRepository(
        IMessageContractRepository messageContractRepository
    )
    {
        _messageContractRepository = messageContractRepository;
        return this;
    }

    public KafkaTopicApplicationServiceBuilder WithSystemTime(DateTime systemTime)
    {
        _systemTime = new SystemTime(() => systemTime);
        return this;
    }

    public KafkaTopicApplicationServiceBuilder WithKafkaTopicRepository(IKafkaTopicRepository kafkaTopicRepository)
    {
        _kafkaTopicRepository = kafkaTopicRepository;
        return this;
    }

    public SelfService.Application.KafkaTopicApplicationService Build()
    {
        return new SelfService.Application.KafkaTopicApplicationService(
            logger: NullLogger<SelfService.Application.KafkaTopicApplicationService>.Instance,
            messageContractRepository: _messageContractRepository,
            systemTime: _systemTime,
            kafkaTopicRepository: _kafkaTopicRepository
        );
    }

    public static implicit operator SelfService.Application.KafkaTopicApplicationService(
        KafkaTopicApplicationServiceBuilder builder
    ) => builder.Build();
}
