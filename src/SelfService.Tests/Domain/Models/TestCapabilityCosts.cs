using System;
using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestCapabilityCosts
{
    private static CapabilityCosts Costs(params TimeSeries[] points) =>
        new(CapabilityId.Parse("test-capability-abc12"), points);

    [Fact]
    public void SumForLastDays_NoData_ReturnsNull()
    {
        Assert.Null(Costs().SumForLastDays(7));
    }

    [Fact]
    public void SumForLastDays_FewerDaysThanWindow_SumsAll()
    {
        var costs = Costs(
            new TimeSeries(10.00f, new DateTime(2026, 6, 27)),
            new TimeSeries(5.50f, new DateTime(2026, 6, 28))
        );

        Assert.Equal(15.50f, costs.SumForLastDays(7));
    }

    [Fact]
    public void SumForLastDays_MoreDaysThanWindow_SumsMostRecentUnordered()
    {
        // Deliberately unordered input; the 7-day window must pick the 7 most recent timestamps.
        var costs = Costs(
            new TimeSeries(1f, new DateTime(2026, 6, 20)), // older — excluded by a 3-day window
            new TimeSeries(1f, new DateTime(2026, 6, 21)), // older — excluded
            new TimeSeries(2f, new DateTime(2026, 6, 28)), // newest
            new TimeSeries(4f, new DateTime(2026, 6, 26)),
            new TimeSeries(8f, new DateTime(2026, 6, 27))
        );

        // 3 most recent: 6/28 (2) + 6/27 (8) + 6/26 (4) = 14
        Assert.Equal(14f, costs.SumForLastDays(3));
    }
}
