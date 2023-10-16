using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Tests.Comparers;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestMembershipApplicationRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_inserts_expected_capability_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubApprovals = new[] { A.MembershipApproval.Build(), A.MembershipApproval.Build(), };

        var stub = A.MembershipApplication
        //.WithApprovals(stubApprovals)
        .Build();

        stub.Approve(UserId.Parse("foo1"), DateTime.MinValue);
        stub.Approve(UserId.Parse("foo2"), DateTime.MinValue);

        var sut = A.MembershipApplicationRepository.WithDbContext(dbContext).Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.MembershipApplications.ToListAsync());
        Assert.Equal(stub, inserted, new MembershipApplicationComparer());
        Assert.Equal(stubApprovals.Length, stub.Approvals.Count());
    }
}
