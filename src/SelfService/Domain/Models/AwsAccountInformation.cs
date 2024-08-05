using Amazon.EC2;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class AwsAccountInformation : AggregateRoot<AwsAccountId>
{
    public AwsAccountInformation(AwsAccountId id, CapabilityId capabilityId, List<VPCInformation> vpclist)
        : base(id)
    {
        CapabilityId = capabilityId;
        vpcs = vpclist;
    }

    public List<VPCInformation> vpcs { get; private set; } = new List<VPCInformation>();
    public CapabilityId CapabilityId { get; private set; }
}
