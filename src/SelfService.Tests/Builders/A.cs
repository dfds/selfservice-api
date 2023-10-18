using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Application.KafkaTopicApplicationService;

namespace SelfService.Tests.Builders;

public static class A
{
    public static UserId UserId => UserId.Parse("foo");

    public static CapabilityBuilder Capability => new();
    public static MembershipBuilder Membership => new();
    public static MemberBuilder Member => new();
    public static AwsAccountBuilder AwsAccount => new();
    public static KafkaClusterBuilder KafkaCluster => new();
    public static KafkaTopicBuilder KafkaTopic => new();
    public static MembershipApplicationBuilder MembershipApplication => new();
    public static MembershipApprovalBuilder MembershipApproval => new();
    public static MessageContractBuilder MessageContract => new();

    public static CapabilityRepositoryBuilder CapabilityRepository => new();
    public static MembershipApplicationRepositoryBuilder MembershipApplicationRepository => new();

    public static KafkaTopicApplicationServiceBuilder KafkaTopicApplicationService => new();
    public static MembershipRepositoryBuilder MembershipRepository => new();
    public static DeactivatedMemberCleanerApplicationServiceBuilder DeactivatedMemberCleanerApplicationService => new();
    public static SelfServiceJsonSchemaServiceBuilder SelfServiceJsonSchemaService => new();
    public static CapabilityApplicationServiceBuilder CapabilityApplicationService => new();

    public static ECRRepositoryBuilder ECRRepository => new();
    public static ECRRepositoryRepositoryBuilder ECRRepositoryRepository => new();

    public static TeamBuilder Team = new();
    public static TeamApplicationServiceBuilder TeamApplicationService = new();
}
