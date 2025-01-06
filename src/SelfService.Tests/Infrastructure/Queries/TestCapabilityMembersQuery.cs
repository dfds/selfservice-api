using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Tests.Infrastructure.Queries;

public class TestCapabilityMembersQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_when_no_capabilities_match()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var sut = new CapabilityMembersQuery(dbContext);

        var result = await sut.FindBy(CapabilityId.CreateFrom("foo"));
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_members_for_a_capability_with_no_members()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubCapability = A.Capability.Build();

        await dbContext.Capabilities.AddAsync(stubCapability, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new CapabilityMembersQuery(dbContext);
        var result = await sut.FindBy(stubCapability.Id);

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_members_for_a_capability_with_single_membership()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expectedMember = A.Member.Build();
        var stubOtherMember = A.Member.WithUserId("other-member-id").Build();

        var stubCapability = A.Capability.Build();
        var stubMembership = A.Membership.WithCapabilityId(stubCapability.Id).WithUserId(expectedMember.Id).Build();

        await dbContext.Capabilities.AddAsync(stubCapability, cancellationTokenSource.Token);
        await dbContext.Members.AddAsync(expectedMember, cancellationTokenSource.Token);
        await dbContext.Members.AddAsync(stubOtherMember, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);
        await dbContext.Memberships.AddAsync(stubMembership, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new CapabilityMembersQuery(dbContext);
        var result = await sut.FindBy(stubCapability.Id);

        Assert.Equal(new[] { expectedMember }, result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_members_for_a_capability_with_multiple_memberships()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expectedMembers = new[] { A.Member.WithUserId("1").Build(), A.Member.WithUserId("2").Build() };

        var stubCapability = A.Capability.Build();

        var stubMemberships = expectedMembers
            .Select(x => A.Membership.WithCapabilityId(stubCapability.Id).WithUserId(x.Id).Build())
            .ToArray();

        await dbContext.Members.AddRangeAsync(expectedMembers, cancellationTokenSource.Token);
        await dbContext.Capabilities.AddAsync(stubCapability, cancellationTokenSource.Token);
        await dbContext.Memberships.AddRangeAsync(stubMemberships, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new CapabilityMembersQuery(dbContext);
        var result = await sut.FindBy(stubCapability.Id);

        Assert.Equal(expectedMembers, result);
    }
}

public class TestMyCapabilitiesQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_when_user_is_unknown()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var sut = new MyCapabilitiesQuery(dbContext);

        var result = await sut.FindBy(UserId.Parse("foo"));
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_when_user_exists_but_has_not_capabilities()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubMember = A.Member.Build();
        await dbContext.Members.AddAsync(stubMember, cancellationTokenSource.Token);

        var sut = new MyCapabilitiesQuery(dbContext);

        var result = await sut.FindBy(stubMember.Id);
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_when_is_member_of_a_capability()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubMember = A.Member.Build();
        var stubCapability = A.Capability.Build();
        var stubMembership = A.Membership.WithUserId(stubMember.Id).WithCapabilityId(stubCapability.Id).Build();

        await dbContext.Members.AddAsync(stubMember, cancellationTokenSource.Token);
        await dbContext.Capabilities.AddAsync(stubCapability, cancellationTokenSource.Token);
        await dbContext.Memberships.AddAsync(stubMembership, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new MyCapabilitiesQuery(dbContext);
        var result = await sut.FindBy(stubMember.Id);

        Assert.Equal(new[] { stubCapability }, result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_when_is_member_of_multiple_capabilities()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubMember = A.Member.Build();

        var expectedCapabilities = new[]
        {
            A.Capability.WithId("a").WithName("a").Build(),
            A.Capability.WithId("b").WithName("b").Build(),
        };

        var stubOtherCapability = A.Capability.WithId("not-this-one").Build();

        var stubMembership = expectedCapabilities
            .Select(x => A.Membership.WithUserId(stubMember.Id).WithCapabilityId(x.Id).Build())
            .ToArray();

        await dbContext.Members.AddAsync(stubMember, cancellationTokenSource.Token);

        await dbContext.Capabilities.AddAsync(stubOtherCapability, cancellationTokenSource.Token);
        await dbContext.Capabilities.AddRangeAsync(expectedCapabilities, cancellationTokenSource.Token);
        await dbContext.Memberships.AddRangeAsync(stubMembership, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new MyCapabilitiesQuery(dbContext);
        var result = await sut.FindBy(stubMember.Id);

        Assert.Equal(expectedCapabilities, result);
    }
}
