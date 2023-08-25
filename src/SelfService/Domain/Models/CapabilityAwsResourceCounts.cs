namespace SelfService.Domain.Models;

public class AwsResourceCount
{
    public string ResourceId { get; set; }
    public int Count { get; set; }

    public AwsResourceCount(string resourceId, int count)
    {
        ResourceId = resourceId;
        Count = count;
    }
}

public class CapabilityAwsResourceCounts
{
    public string CapabilityId { get; set; }
    public List<AwsResourceCount> AwsResourceCounts { get; set; }

    public CapabilityAwsResourceCounts(CapabilityId capabilityId, List<AwsResourceCount> awsResourceCount)
    {
        CapabilityId = capabilityId;
        AwsResourceCounts = awsResourceCount;
    }
}

public class MyCapabilitiesAwsResourceCounts
{
    private List<CapabilityAwsResourceCounts> CapabilityAwsResourceCounts { get; set; }

    public MyCapabilitiesAwsResourceCounts(List<CapabilityAwsResourceCounts> capabilityAwsResourceCounts)
    {
        CapabilityAwsResourceCounts = capabilityAwsResourceCounts;
    }
}
