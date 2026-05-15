using Dafda.Consuming;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Domain.Policies;

namespace SelfService.Tests.Domain.Policies;

public class TestMarkCapabilityAsDeletedHandler
{
    private static MarkCapabilityAsDeletedHandler BuildHandler(ICapabilityApplicationService service) =>
        new MarkCapabilityAsDeletedHandler(NullLogger<MarkCapabilityAsDeletedHandler>.Instance, service);

#pragma warning disable CS0618
    private static readonly MessageHandlerContext EmptyContext = new MessageHandlerContext();
#pragma warning restore CS0618

    [Fact]
    public async Task calls_mark_as_deleted_with_correct_capability_id()
    {
        var capabilityId = "my-capability";
        var appServiceMock = new Mock<ICapabilityApplicationService>();

        var sut = BuildHandler(appServiceMock.Object);

        await sut.Handle(
            new CapabilityReadyForDeletion { CapabilityId = capabilityId },
            EmptyContext
        );

        appServiceMock.Verify(
            x => x.MarkCapabilityAsDeleted(CapabilityId.Parse(capabilityId)),
            Times.Once
        );
    }

    [Fact]
    public async Task does_not_call_service_when_capability_id_is_null()
    {
        var appServiceMock = new Mock<ICapabilityApplicationService>();

        var sut = BuildHandler(appServiceMock.Object);

        await sut.Handle(
            new CapabilityReadyForDeletion { CapabilityId = null },
            EmptyContext
        );

        appServiceMock.Verify(x => x.MarkCapabilityAsDeleted(It.IsAny<CapabilityId>()), Times.Never);
    }

    [Fact]
    public async Task does_not_call_service_when_capability_id_is_empty()
    {
        var appServiceMock = new Mock<ICapabilityApplicationService>();

        var sut = BuildHandler(appServiceMock.Object);

        await sut.Handle(
            new CapabilityReadyForDeletion { CapabilityId = "" },
            EmptyContext
        );

        appServiceMock.Verify(x => x.MarkCapabilityAsDeleted(It.IsAny<CapabilityId>()), Times.Never);
    }
}
