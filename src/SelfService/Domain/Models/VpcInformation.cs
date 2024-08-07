namespace SelfService.Domain.Models;

public class VPCInformation
{
    public string VpcId { get; set; } = null!;
    public string CidrBlock { get; set; } = null!;
    public string Region { get; set; } = null!;

    public override string ToString()
    {
        return $"VPC ID: {VpcId}, CIDR Block: {CidrBlock}, Region: {Region}";
    }
}
