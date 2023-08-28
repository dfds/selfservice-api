namespace SelfService.Domain.Models;

public class AwsResourceCount
{
    public string ResourceId { get; set; }
    public int ResourceCount { get; set; }

    public AwsResourceCount(string resourceId, int resourceCount)
    {
        ResourceId = resourceId;
        ResourceCount = resourceCount;
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
    public List<CapabilityAwsResourceCounts> CapabilityAwsResourceCounts { get; set; }

    public MyCapabilitiesAwsResourceCounts(List<CapabilityAwsResourceCounts> capabilityAwsResourceCounts)
    {
        CapabilityAwsResourceCounts = capabilityAwsResourceCounts;
    }
}
