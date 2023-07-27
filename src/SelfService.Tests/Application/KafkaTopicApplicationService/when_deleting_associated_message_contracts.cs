using Moq;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application.KafkaTopicApplicationService;

public class when_deleting_associated_message_contracts: IAsyncLifetime
{
    private readonly MessageContract _aMessageContract = A.MessageContract.Build();
    private readonly Mock<IMessageContractRepository> _mock = new();

    public async Task InitializeAsync()
    {
        _mock
            .Setup(x => x.FindBy(It.IsAny<KafkaTopicId>()))
            .ReturnsAsync(new[] {_aMessageContract});

        var sut = A.KafkaTopicApplicationService
            .WithMessageContractRepository(_mock.Object)
            .Build();

        await sut.DeleteAssociatedMessageContracts(_aMessageContract.KafkaTopicId, "dummy-user");
    }

    [Fact]
    public void then_the_message_is_deleted_in_the_repository()
    {
        _mock.Verify(x => x.Delete(_aMessageContract), Times.Once());
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}