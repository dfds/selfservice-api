namespace SelfService.Domain.Models;


public class Timeseries
{
    public DateTime TimeStamp;
    public string Value;
}

public class CapabilityCosts
{
    private readonly Capability _capability;
    private readonly Timeseries[] _costs;

    public CapabilityCosts(Capability capability, Timeseries[] costs)
    {
        _capability = capability;
        _costs = costs;
    }   
}