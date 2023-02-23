namespace SelfService.Tests.Builders;

public static class A
{
    public static CapabilityBuilder Capability => new();
    public static MembershipBuilder Membership => new();
    public static MemberBuilder Member => new();

    public static AwsAccountBuilder AwsAccount => new();
    
    public static CapabilityRepositoryBuilder CapabilityRepository => new();
}