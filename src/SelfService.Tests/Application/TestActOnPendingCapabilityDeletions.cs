using Moq;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestActOnPendingCapabilityDeletions
{
    private static CapabilityApplicationService BuildServiceWithCapabilities(
        IEnumerable<Capability> pendingCapabilities
    )
    {
        var repoMock = new Mock<ICapabilityRepository>();
        repoMock
            .Setup(r => r.GetAllPendingDeletionFor(It.IsAny<int>()))
            .ReturnsAsync(pendingCapabilities);

        return A.CapabilityApplicationService.WithCapabilityRepository(repoMock.Object).Build();
    }

    [Fact]
    public async Task raises_capability_ready_for_deletion_event_for_old_pending_capability()
    {
        var capability = A.Capability
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
            .Build();

        var sut = BuildServiceWithCapabilities([capability]);

        await sut.ActOnPendingCapabilityDeletions();

        var events = capability.GetEvents().ToList();
        var evt = Assert.Single(events);
        var readyForDeletion = Assert.IsType<CapabilityReadyForDeletion>(evt);
        Assert.Equal(capability.Id.ToString(), readyForDeletion.CapabilityId);
    }

    [Fact]
    public async Task sets_capability_status_to_ongoing_deletion()
    {
        var capability = A.Capability
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
            .Build();

        var sut = BuildServiceWithCapabilities([capability]);

        await sut.ActOnPendingCapabilityDeletions();

        Assert.Equal(CapabilityStatusOptions.OngoingDeletion, capability.Status);
    }

    [Fact]
    public async Task raises_event_for_each_pending_capability()
    {
        var capability1 = A.Capability
            .WithId("cap-one")
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
            .Build();

        var capability2 = A.Capability
            .WithId("cap-two")
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-10))
            .Build();

        var sut = BuildServiceWithCapabilities([capability1, capability2]);

        await sut.ActOnPendingCapabilityDeletions();

        Assert.Single(capability1.GetEvents());
        Assert.Single(capability2.GetEvents());
        Assert.Equal(CapabilityStatusOptions.OngoingDeletion, capability1.Status);
        Assert.Equal(CapabilityStatusOptions.OngoingDeletion, capability2.Status);
    }

    [Fact]
    public async Task does_nothing_when_no_pending_capabilities()
    {
        var sut = BuildServiceWithCapabilities([]);

        // Should complete without throwing
        await sut.ActOnPendingCapabilityDeletions();
    }
}
