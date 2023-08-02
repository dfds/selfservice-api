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
    public CapabilityId CapabilityId { get; set; }
    public TimeSeries[] Costs { get; set; }

    public CapabilityCosts(CapabilityId capabilityId, TimeSeries[] costs)
    {
        CapabilityId = capabilityId;
        Costs = costs;
    }
}
