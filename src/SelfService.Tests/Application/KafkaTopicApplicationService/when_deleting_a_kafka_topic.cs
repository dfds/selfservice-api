using Moq;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Application.KafkaTopicApplicationService;

public class when_deleting_a_kafka_topic: IAsyncLifetime
{
    private readonly Mock<IKafkaTopicRepository> _stubAndMockRepository = new();
    private readonly KafkaTopic _aTopic = A.KafkaTopic.Build();

    public async Task InitializeAsync()
    {
        var stubKafkaTopicRepository = new StubKafkaTopicRepository(_aTopic);

        _stubAndMockRepository
            .Setup(x => x.Get(_aTopic.Id))
            .ReturnsAsync(_aTopic);

        var sut = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(_stubAndMockRepository.Object)
            .Build();

        await sut.DeleteKafkaTopic(_aTopic.Id, "dummy-user");
    }

    [Fact]
    public void then_expected_domain_event_is_raised()
    {
        var domainEvent = Assert.Single(_aTopic.GetEvents());
        var deletedEvent = Assert.IsType<KafkaTopicHasBeenDeleted>(domainEvent);
        Assert.Equal(_aTopic.Id, deletedEvent.KafkaTopicId);
    }

    [Fact]
    public void then_topic_is_deleted_from_repository()
    {
        _stubAndMockRepository.Verify(x => x.Delete(_aTopic), Times.Once);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}