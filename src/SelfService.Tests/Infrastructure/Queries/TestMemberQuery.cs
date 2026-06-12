using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Tests.Infrastructure.Queries;

public class TestMemberQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task search_returns_empty_when_no_members_exist()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var sut = new MemberQuery(dbContext);
        var (items, total) = await sut.Search(type: null, search: null, limit: 50, offset: 0);

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task search_returns_all_when_no_filter()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        await dbContext.Members.AddRangeAsync(
            A.Member.WithUserId("alice@dfds.com").WithEmail("alice@dfds.com").Build(),
            A.Member.WithUserId("bob@dfds.com").WithEmail("bob@dfds.com").Build()
        );
        await dbContext.SaveChangesAsync();

        var sut = new MemberQuery(dbContext);
        var (items, total) = await sut.Search(type: null, search: null, limit: 50, offset: 0);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, total);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task search_filters_by_type_servicePrincipal()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        await dbContext.Members.AddRangeAsync(
            A.Member.WithUserId("alice@dfds.com").WithEmail("alice@dfds.com").Build(),
            A.Member.WithUserId("sp-0001")
                .WithEmail("sp-0001@service.local")
                .WithDisplayName("Worker SP")
                .AsServicePrincipal()
                .Build()
        );
        await dbContext.SaveChangesAsync();

        var sut = new MemberQuery(dbContext);
        var (items, total) = await sut.Search(MemberType.ServicePrincipal, search: null, limit: 50, offset: 0);

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(MemberType.ServicePrincipal, items[0].Type);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task search_filters_by_substring_in_email_or_display_name()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        await dbContext.Members.AddRangeAsync(
            A.Member.WithUserId("alice@dfds.com").WithEmail("alice@dfds.com").WithDisplayName("Alice").Build(),
            A.Member.WithUserId("bob@external.com").WithEmail("bob@external.com").WithDisplayName("Bob").Build()
        );
        await dbContext.SaveChangesAsync();

        var sut = new MemberQuery(dbContext);
        var (items, total) = await sut.Search(type: null, search: "alice", limit: 50, offset: 0);

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal("alice@dfds.com", items[0].Email);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task search_clamps_limit_and_paginates()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        for (var i = 0; i < 5; i++)
        {
            await dbContext.Members.AddAsync(
                A.Member.WithUserId($"user-{i:D2}@dfds.com").WithEmail($"user-{i:D2}@dfds.com").Build()
            );
        }
        await dbContext.SaveChangesAsync();

        var sut = new MemberQuery(dbContext);
        var (items, total) = await sut.Search(type: null, search: null, limit: 2, offset: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal(5, total);
        Assert.Equal("user-02@dfds.com", items[0].Email);
        Assert.Equal("user-03@dfds.com", items[1].Email);
    }
}
