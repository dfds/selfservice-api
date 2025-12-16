using System.Net.NetworkInformation;
using JetBrains.Annotations;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Application.KafkaTopicApplicationService;

namespace SelfService.Tests.Builders;

public static class A
{
    public static UserId UserId => UserId.Parse("foo");
    public static CapabilityBuilder Capability => new();
    public static CapabilityRequestBuilder CapabilityRequest => new();
    public static MembershipBuilder Membership => new();
    public static MemberBuilder Member => new();
    public static AwsAccountBuilder AwsAccount => new();
    public static AzureResourceBuilder AzureResource => new();
    public static KafkaClusterBuilder KafkaCluster => new();
    public static KafkaTopicBuilder KafkaTopic => new();
    public static MembershipApplicationBuilder MembershipApplication => new();
    public static MembershipApprovalBuilder MembershipApproval => new();
    public static MessageContractBuilder MessageContract => new();
    public static ReleaseNoteBuilder ReleaseNote => new();
    public static DemoRecordingBuilder DemoRecording => new();

    public static CapabilityRepositoryBuilder CapabilityRepository => new();
    public static MembershipApplicationRepositoryBuilder MembershipApplicationRepository => new();
    public static ReleaseNoteRepositoryBuilder ReleaseNoteRepository => new();

    public static KafkaTopicApplicationServiceBuilder KafkaTopicApplicationService => new();
    public static MembershipRepositoryBuilder MembershipRepository => new();
    public static DeactivatedMemberCleanerApplicationServiceBuilder DeactivatedMemberCleanerApplicationService => new();
    public static SelfServiceJsonSchemaServiceBuilder SelfServiceJsonSchemaService => new();
    public static CapabilityApplicationServiceBuilder CapabilityApplicationService => new();

    public static ECRRepositoryBuilder ECRRepository => new();
    public static ECRRepositoryRepositoryBuilder ECRRepositoryRepository => new();

    public static TeamBuilder Team = new();
    public static TeamApplicationServiceBuilder TeamApplicationService = new();
    public static MembershipApplicationServiceBuilder MembershipApplicationService => new();

    public static ConfigurationLevelServiceBuilder ConfigurationLevelService => new();

    public static DemoRecordingRepositoryBuilder DemoRecordingRepository => new();
    public static DemoRecordingServiceBuilder DemoRecordingService => new();

    public static RbacRoleBuilder RbacRole => new();
    public static RbacRoleGrantBuilder RbacRoleGrant => new();
}
