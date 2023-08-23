using Castle.Components.DictionaryAdapter;

namespace SelfService.Domain.Models;

public class MyCapabilitiesMetrics
{
    public List<CapabilityCosts> Costs { get; set; }
    public List<CapabilityAwsResourceCounts> AwsResourceCountsList { get; set; }

    public MyCapabilitiesMetrics(List<CapabilityCosts> costs, List<CapabilityAwsResourceCounts> awsResourceCountsLists)
    {
        Costs = costs;
        AwsResourceCountsList = awsResourceCountsLists;
    }
}

public class EmptyMyCapabilitiesMetrics : MyCapabilitiesMetrics
{
    public EmptyMyCapabilitiesMetrics()
        : base(new List<CapabilityCosts>(), new EditableList<CapabilityAwsResourceCounts>()) { }
}
