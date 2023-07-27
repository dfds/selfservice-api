using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Application.KafkaTopicApplicationService;

public class when_changing_kafka_topic_description : IAsyncLifetime
{
    private readonly KafkaTopic _aTopic = A.KafkaTopic
        .WithDescription("foo")
        .Build();

    public async Task InitializeAsync()
    {
        var sut = A.KafkaTopicApplicationService
            .WithKafkaTopicRepository(new StubKafkaTopicRepository(_aTopic))
            .WithSystemTime(new DateTime(2000, 1, 1))
            .Build();

        await sut.ChangeKafkaTopicDescription(_aTopic.Id, "bar", "foo-user");
    }

    [Fact]
    public void description_is_set_to_the_expected()
    {
        Assert.Equal("bar", _aTopic.Description);
    }

    [Fact]
    public void modified_by_is_set_to_the_expected()
    {
        Assert.Equal("foo-user", _aTopic.ModifiedBy);
    }

    [Fact]
    public void modified_at_is_set_to_the_expected()
    {
        Assert.Equal(new DateTime(2000, 1, 1), _aTopic.ModifiedAt);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}