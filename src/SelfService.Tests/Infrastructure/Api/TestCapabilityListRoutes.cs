using System.Text.Json;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestCapabilityListRoutes
{
    [Fact]
    public async Task get_all_capabilities_returns_the_capability_list_resource()
    {
        var stubCapability = A.Capability.WithId("foo").Build();

        var capabilityRepository = new Mock<ICapabilityRepository>();
        capabilityRepository.Setup(x => x.GetAll()).ReturnsAsync(new[] { stubCapability });
        var membershipRepository = new Mock<IMembershipRepository>();
        membershipRepository
            .Setup(x => x.GetAllMembershipsForUserId(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Membership>());
        var favouriteRepository = new Mock<IFavouriteRepository>();
        favouriteRepository
            .Setup(x => x.GetAllFavouritesForUserId(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Favourite>());
        var awsAccountIdQuery = new Mock<IAwsAccountIdQuery>();
        awsAccountIdQuery
            .Setup(x => x.FindBy(It.IsAny<IReadOnlyCollection<CapabilityId>>()))
            .ReturnsAsync(new Dictionary<CapabilityId, RealAwsAccountId>());

        await using var application = new ApiApplicationBuilder()
            .WithCapabilityRepository(capabilityRepository.Object)
            .WithFavouriteRepository(favouriteRepository.Object)
            .Build();
        application.ReplaceService<IMembershipRepository>(membershipRepository.Object);
        application.ReplaceService<IAwsAccountIdQuery>(awsAccountIdQuery.Object);
        application.ReplaceService<IRbacApplicationService>(new StubRbacApplicationService(isPermitted: false));

        using var client = application.CreateClient();
        var response = await client.GetAsync("/capabilities");

        response.EnsureSuccessStatusCode();
        var document = JsonSerializer.Deserialize<JsonDocument>(await response.Content.ReadAsStringAsync());

        var ids = document
            ?.SelectElement("/items")
            ?.EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .ToArray();
        Assert.Equal(new[] { "foo" }, ids);
        Assert.EndsWith("/capabilities", document?.SelectElement("/_links/self/href")?.GetString());
    }
}
