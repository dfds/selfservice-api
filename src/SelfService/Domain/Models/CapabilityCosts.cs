namespace SelfService.Domain.Models;

public class TimeSeries
{
    public DateTime TimeStamp { get; set; }
    public float Value { get; set; }

    public TimeSeries(float value, DateTime timeStamp)
    {
        Value = value;
        TimeStamp = timeStamp;
    }
}

public class CapabilityCosts
{
    public string CapabilityId { get; set; }
    public TimeSeries[] Costs { get; set; }

    public CapabilityCosts(CapabilityId capabilityId, TimeSeries[] costs)
    {
        CapabilityId = capabilityId;
        Costs = costs;
    }

    /// <summary>
    /// Sum of the N most recent daily cost datapoints, or null when there is no data.
    /// The cached time series already excludes today/yesterday (incomplete days), so this
    /// mirrors how the portal computes a rolling window total (sum of daily values).
    /// </summary>
    public float? SumForLastDays(int days)
    {
        if (Costs is null || Costs.Length == 0)
            return null;
        return Costs.OrderByDescending(c => c.TimeStamp).Take(days).Sum(c => c.Value);
    }
}

public class MyCapabilityCosts
{
    public List<CapabilityCosts> Costs { get; set; }

    public MyCapabilityCosts(List<CapabilityCosts> costs)
    {
        Costs = costs;
    }
}

public class AllCapabilitiesCosts
{
    public List<CapabilityCosts> Costs { get; set; }

    public AllCapabilitiesCosts(List<CapabilityCosts> costs)
    {
        Costs = costs;
    }
}
