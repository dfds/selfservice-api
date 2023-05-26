using System.Net;
using SelfService.Domain.Models;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api.CapabilityTopicsRoutes;

public class when_getting_topics_for_an_unknown_capability : IAsyncLifetime
{
    private HttpResponseMessage _response = null!;

    public async Task InitializeAsync()
    {
        await using var application = new ApiApplication();
        application.ReplaceService<ICapabilityRepository>(new StubCapabilityRepository());

        using var client = application.CreateClient();
        _response = await client.GetAsync($"/capabilities/foo/topics");
    }

    [Fact]
    public void then_returns_expected_response_status_code()
    {
        Assert.Equal((HttpStatusCode)404, _response.StatusCode);
    }

    public Task DisposeAsync()
    {
        _response!.Dispose();
        return Task.CompletedTask;
    }
}