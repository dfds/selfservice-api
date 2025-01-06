using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using PlatformDataApiTimeSeries = SelfService.Application.PlatformDataApiRequesterService.PlatformDataApiTimeSeries;

namespace SelfService.Tests.Application;

public class TestPlatformDataApiRequesterService
{
    [Fact]
    public async Task get_correct_date_ordering()
    {
        var myCapabilitiesQueryMock = new Mock<IMyCapabilitiesQuery>();
        var capability = A.Capability.Build();
        myCapabilitiesQueryMock.Setup(x => x.FindBy(A.UserId)).ReturnsAsync(new List<Capability> { capability });

        var earliest = DateTime.UtcNow.AddDays(-5);
        var mockSeries = new PlatformDataApiTimeSeries[]
        {
            new()
            {
                TimeStamp = earliest.AddDays(3),
                Value = 0.1f,
                Tag = capability.Id,
            },
            new()
            {
                TimeStamp = earliest.AddDays(1),
                Value = 0.1f,
                Tag = capability.Id,
            },
            new()
            {
                TimeStamp = earliest.AddDays(4),
                Value = 0.1f,
                Tag = capability.Id,
            },
            new()
            {
                TimeStamp = DateTime.UtcNow.AddDays(2),
                Value = 0.1f,
                Tag = capability.Id,
            },
        };

        string mockResponse = JsonSerializer.Serialize(mockSeries);
        var httpClient = HttpHelper.CreateMockHttpClient(mockResponse);
        var loggerMock = new Mock<ILogger<PlatformDataApiRequesterService>>();
        var awsMock = new Mock<IAwsAccountRepository>();
        var azureMock = new Mock<IAzureResourceRepository>();

        var service = new PlatformDataApiRequesterService(
            loggerMock.Object,
            awsMock.Object,
            myCapabilitiesQueryMock.Object,
            httpClient
        );

        var costs = await service.GetMyCapabilitiesCosts(A.UserId);
        Assert.Single(costs.Costs);
        Assert.Equal(4, costs.Costs[0].Costs.Length);

        var capabilityCosts = costs.Costs[0].Costs;
        var previousTime = earliest;
        foreach (var timeSeries in capabilityCosts)
        {
            Assert.True(timeSeries.TimeStamp >= previousTime);
            previousTime = timeSeries.TimeStamp;
        }
    }
}
