using Dafda.Consuming;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Domain.Policies;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Application;

public class TestCapabilityDeletionIntegration
{
    private static (CapabilityRepository, CapabilityApplicationService) BuildService(
        SelfServiceDbContext dbContext
    )
    {
        var repo = A.CapabilityRepository.WithDbContext(dbContext).Build();
        var service = A.CapabilityApplicationService.WithCapabilityRepository(repo).Build();
        return (repo, service);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task act_on_pending_deletions_transitions_old_capability_to_ongoing_deletion_in_db()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (repo, service) = BuildService(dbContext);

        var capability = A.Capability
            .WithId("pending-old")
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
            .Build();
        await repo.Add(capability);
        await dbContext.SaveChangesAsync();

        await service.ActOnPendingCapabilityDeletions();
        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Capabilities.SingleAsync(c => c.Id == capability.Id);
        Assert.Equal(CapabilityStatusOptions.OngoingDeletion, stored.Status);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task act_on_pending_deletions_does_not_affect_recently_pending_capability()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (repo, service) = BuildService(dbContext);

        var recentPending = A.Capability
            .WithId("pending-new")
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-1))
            .Build();
        await repo.Add(recentPending);
        await dbContext.SaveChangesAsync();

        await service.ActOnPendingCapabilityDeletions();
        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Capabilities.SingleAsync(c => c.Id == recentPending.Id);
        Assert.Equal(CapabilityStatusOptions.PendingDeletion, stored.Status);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task act_on_pending_deletions_raises_ready_for_deletion_event()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (repo, service) = BuildService(dbContext);

        var capability = A.Capability
            .WithId("pending-old")
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
            .Build();
        await repo.Add(capability);
        await dbContext.SaveChangesAsync();

        await service.ActOnPendingCapabilityDeletions();

        var evt = Assert.Single(capability.GetEvents());
        var readyForDeletion = Assert.IsType<CapabilityReadyForDeletion>(evt);
        Assert.Equal(capability.Id.ToString(), readyForDeletion.CapabilityId);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task mark_capability_as_deleted_transitions_ongoing_deletion_to_deleted_in_db()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (repo, service) = BuildService(dbContext);

        var capability = A.Capability
            .WithId("ongoing-cap")
            .WithStatus(CapabilityStatusOptions.OngoingDeletion)
            .Build();
        await repo.Add(capability);
        await dbContext.SaveChangesAsync();

        await service.MarkCapabilityAsDeleted(capability.Id);
        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Capabilities.SingleAsync(c => c.Id == capability.Id);
        Assert.Equal(CapabilityStatusOptions.Deleted, stored.Status);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task handler_marks_capability_as_deleted_in_db_when_receiving_ready_for_deletion_event()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var (repo, service) = BuildService(dbContext);

        var capability = A.Capability
            .WithId("ongoing-cap")
            .WithStatus(CapabilityStatusOptions.OngoingDeletion)
            .Build();
        await repo.Add(capability);
        await dbContext.SaveChangesAsync();

#pragma warning disable CS0618
        var handler = new MarkCapabilityAsDeletedHandler(
            NullLogger<MarkCapabilityAsDeletedHandler>.Instance,
            service
        );
        await handler.Handle(
            new CapabilityReadyForDeletion { CapabilityId = capability.Id },
            new MessageHandlerContext()
        );
#pragma warning restore CS0618

        await dbContext.SaveChangesAsync();

        var stored = await dbContext.Capabilities.SingleAsync(c => c.Id == capability.Id);
        Assert.Equal(CapabilityStatusOptions.Deleted, stored.Status);
    }
}
