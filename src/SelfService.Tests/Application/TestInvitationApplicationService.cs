using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestInvitationApplicationService
{
    const string dummyInviterId = "dummyInviterId";
    const string dummyInvitee1 = "dummyInvitee1";
    UserId dummyInvitee1Id = UserId.Parse(dummyInvitee1);
    const string dummyInvitee2 = "dummyInvitee2";

    [Fact]
    public async Task add_invitations_for_capability()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        var invitations = await invitationRepo.GetAllWithPredicate(x =>
            x.TargetId == testCapability.Id && x.TargetType == InvitationTargetTypeOptions.Capability
        );
        Assert.Equal(2, invitations.Count);
    }

    [Fact]
    public async Task silently_ignore_identical_simultaneous_invitations()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1, dummyInvitee2, dummyInvitee1 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        var invitations = await invitationRepo.GetAllWithPredicate(x =>
            x.TargetId == testCapability.Id && x.TargetType == InvitationTargetTypeOptions.Capability
        );
        Assert.Equal(2, invitations.Count);
    }

    [Fact]
    public async Task ignore_adding_identical_invitation()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees;
        List<Invitation> invitations;

        dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        invitations = await invitationRepo.GetAllWithPredicate(x =>
            x.TargetId == testCapability.Id && x.TargetType == InvitationTargetTypeOptions.Capability
        );
        Assert.Equal(2, invitations.Count);

        dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        invitations = await invitationRepo.GetAllWithPredicate(x =>
            x.TargetId == testCapability.Id && x.TargetType == InvitationTargetTypeOptions.Capability
        );
        Assert.Equal(2, invitations.Count);
    }

    [Fact]
    public async Task get_active_invitations_for_user_and_type()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        var capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Single(capabilityInvitations);

        var unknownInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Unknown
        );
        Assert.Empty(unknownInvitations);
    }

    [Fact]
    public async Task accept_invitation()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        var invitationsSent = await invitationService.CreateCapabilityInvitations(
            dummyInvitees,
            dummyInviterId,
            testCapability
        );
        await dbContext.SaveChangesAsync();

        Assert.Equal(invitationsSent.Count, dummyInvitees.Count);

        var capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Single(capabilityInvitations);

        var acceptedInvitations = await invitationRepo.GetAllWithPredicate(x =>
            x.Status == InvitationStatusOptions.Accepted && x.Invitee == dummyInvitee1Id
        );
        Assert.Empty(acceptedInvitations);

        await invitationService.AcceptInvitation(capabilityInvitations[0].Id);
        await dbContext.SaveChangesAsync();

        acceptedInvitations = await invitationRepo.GetAllWithPredicate(x =>
            x.Status == InvitationStatusOptions.Accepted && x.Invitee == dummyInvitee1Id
        );
        Assert.Single(acceptedInvitations);

        capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Empty(capabilityInvitations);
    }

    [Fact]
    public async Task decline_invitation()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1, dummyInvitee2 };

        var invitationsSent = await invitationService.CreateCapabilityInvitations(
            dummyInvitees,
            dummyInviterId,
            testCapability
        );
        await dbContext.SaveChangesAsync();

        Assert.Equal(invitationsSent.Count, dummyInvitees.Count);

        var capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Single(capabilityInvitations);

        var acceptedInvitations = await invitationRepo.GetAllWithPredicate(x =>
            x.Status == InvitationStatusOptions.Declined && x.Invitee == dummyInvitee1Id
        );
        Assert.Empty(acceptedInvitations);

        await invitationService.DeclineInvitation(capabilityInvitations[0].Id);
        await dbContext.SaveChangesAsync();

        acceptedInvitations = await invitationRepo.GetAllWithPredicate(x =>
            x.Status == InvitationStatusOptions.Declined && x.Invitee == dummyInvitee1Id
        );
        Assert.Single(acceptedInvitations);

        capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Empty(capabilityInvitations);
    }

    [Fact]
    public async Task clear_active_invitations_on_join()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var invitationService = A
            .InvitationApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var membershipService = A
            .MembershipApplicationService.WithMembershipRepository(membershipRepo)
            .WithInvitationRepository(invitationRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        List<string> dummyInvitees = new() { dummyInvitee1 };

        await invitationService.CreateCapabilityInvitations(dummyInvitees, dummyInviterId, testCapability);
        await dbContext.SaveChangesAsync();

        List<Invitation> capabilityInvitations;

        capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Single(capabilityInvitations);

        //join capability
        await membershipService.JoinCapability(testCapability.Id, dummyInvitee1);
        await dbContext.SaveChangesAsync();

        capabilityInvitations = await invitationService.GetActiveInvitationsForType(
            dummyInvitee1Id,
            InvitationTargetTypeOptions.Capability
        );
        Assert.Empty(capabilityInvitations);
    }
}
