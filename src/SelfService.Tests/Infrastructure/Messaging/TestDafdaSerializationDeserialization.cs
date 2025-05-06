using System.Text.Json;
using System.Text.Json.Serialization;
using Dafda.Serializing;
using SelfService.Domain.Events;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Messaging;

namespace SelfService.Tests.Infrastructure.Messaging;

public class TestDafdaSerializationDeserialization
{
    // Copy of code in Dafda.JsonFactory
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private const string TestApplicationId = "applicationId";
    private const string TestCapabilityId = "capabilityId";
    private const string TestUser = "user";
    private const string TestKafkaClusterId = "clusterId";
    private const string TestTopicId = "topicId";
    private const string TestTopicName = "topicName";
    private const string TestMessageType = "messageType";
    private const string TestMessageContractId = "contractId";
    private const string TestPortalVisitId = "portalVisitId";
    private const string TestDescription = "description";
    private const string TestSchema = "schema";
    private const string TestMemberShipId = "membershipId";

    private async Task dafda_serialize_deserialize<T>(T domainEvent)
        where T : IDomainEvent
    {
        DefaultPayloadSerializer serializer = new DefaultPayloadSerializer();
        var serialized = await serializer.Serialize(
            new PayloadDescriptor("", "", "", "", domainEvent, new KeyValuePair<string, string>[] { })
        );

        JsonSerializer.Deserialize(serialized, typeof(T), _jsonSerializerOptions);
    }

    [Fact]
    public async Task dafda_serialize_deserialize_aws_account_requested()
    {
        await dafda_serialize_deserialize(new AwsAccountRequested { AccountId = Guid.NewGuid().ToString() });
        await dafda_serialize_deserialize(new AwsAccountRequested());
    }

    [Fact]
    public async Task dafda_serialize_deserialize_capability_created()
    {
        await dafda_serialize_deserialize(new CapabilityCreated(TestCapabilityId, TestUser));
    }

    [Fact]
    public async Task dafda_serialize_deserialize_kafka_cluster_access_requested()
    {
        await dafda_serialize_deserialize(new KafkaClusterAccessRequested());
        await dafda_serialize_deserialize(
            new KafkaClusterAccessRequested() { CapabilityId = TestCapabilityId, KafkaClusterId = TestKafkaClusterId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_kafka_topic_deleted()
    {
        await dafda_serialize_deserialize(new KafkaTopicHasBeenDeleted());
        await dafda_serialize_deserialize(new KafkaTopicHasBeenDeleted() { KafkaTopicId = TestTopicId });
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_application_cancelled()
    {
        await dafda_serialize_deserialize(new MembershipApplicationHasBeenCancelled());
        await dafda_serialize_deserialize(
            new MembershipApplicationHasBeenCancelled() { MembershipApplicationId = TestApplicationId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_application_finalized()
    {
        await dafda_serialize_deserialize(new MembershipApplicationHasBeenFinalized());
        await dafda_serialize_deserialize(
            new MembershipApplicationHasBeenFinalized() { MembershipApplicationId = TestApplicationId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_application_approval_received()
    {
        await dafda_serialize_deserialize(new MembershipApplicationHasReceivedAnApproval());
        await dafda_serialize_deserialize(
            new MembershipApplicationHasReceivedAnApproval() { MembershipApplicationId = TestApplicationId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_new_kafka_topic_requested()
    {
        await dafda_serialize_deserialize(new NewKafkaTopicHasBeenRequested());
        await dafda_serialize_deserialize(
            new NewKafkaTopicHasBeenRequested
            {
                CapabilityId = TestCapabilityId,
                KafkaClusterId = TestKafkaClusterId,
                KafkaTopicId = TestTopicId,
                KafkaTopicName = TestTopicName,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_new_membership_application_submitted()
    {
        await dafda_serialize_deserialize(new NewMembershipApplicationHasBeenSubmitted());
        await dafda_serialize_deserialize(
            new NewMembershipApplicationHasBeenSubmitted { MembershipApplicationId = TestApplicationId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_new_message_contract_provisioned()
    {
        await dafda_serialize_deserialize(new NewMessageContractHasBeenProvisioned());
        await dafda_serialize_deserialize(
            new NewMessageContractHasBeenProvisioned
            {
                KafkaTopicId = TestTopicId,
                MessageContractId = TestMessageContractId,
                MessageType = TestMessageType,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_new_message_contract_requested()
    {
        await dafda_serialize_deserialize(new NewMessageContractHasBeenRequested());
        await dafda_serialize_deserialize(
            new NewMessageContractHasBeenRequested
            {
                CapabilityId = TestCapabilityId,
                Description = TestDescription,
                KafkaClusterId = TestKafkaClusterId,
                KafkaTopicId = TestTopicId,
                KafkaTopicName = TestTopicName,
                MessageContractId = TestMessageContractId,
                MessageType = TestMessageType,
                Schema = TestSchema,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_new_portal_visit_registered()
    {
        await dafda_serialize_deserialize(new NewPortalVisitRegistered());
        await dafda_serialize_deserialize(
            new NewPortalVisitRegistered
            {
                PortalVisitId = TestPortalVisitId,
                VisitedAt = "now",
                VisitedBy = TestUser,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_user_joined_capability()
    {
        await dafda_serialize_deserialize(new UserHasJoinedCapability());
        await dafda_serialize_deserialize(
            new UserHasJoinedCapability { CapabilityId = TestCapabilityId, MembershipId = TestMemberShipId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_user_left_capability()
    {
        await dafda_serialize_deserialize(new UserHasLeftCapability());
        await dafda_serialize_deserialize(
            new UserHasLeftCapability
            {
                CapabilityId = TestCapabilityId,
                MembershipId = TestMemberShipId,
                UserId = TestUser,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_kafka_cluster_access_granted()
    {
        await dafda_serialize_deserialize(new KafkaClusterAccessGranted());
        await dafda_serialize_deserialize(
            new KafkaClusterAccessGranted { CapabilityId = TestCapabilityId, KafkaClusterId = TestKafkaClusterId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_kafka_topic_provisioning_has_begun()
    {
        await dafda_serialize_deserialize(new KafkaTopicProvisioningHasBegun());
        await dafda_serialize_deserialize(
            new KafkaTopicProvisioningHasBegun
            {
                ClusterId = TestKafkaClusterId,
                TopicId = TestTopicId,
                TopicName = TestTopicName,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_kafka_topic_provisioning_has_completed()
    {
        await dafda_serialize_deserialize(new KafkaTopicProvisioningHasCompleted());
        await dafda_serialize_deserialize(
            new KafkaTopicProvisioningHasCompleted
            {
                ClusterId = TestKafkaClusterId,
                TopicId = TestTopicId,
                TopicName = TestTopicName,
            }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_schema_registered()
    {
        await dafda_serialize_deserialize(new SchemaRegistered());
        await dafda_serialize_deserialize(new SchemaRegistered { MessageContractId = TestMessageContractId });
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_invitation_has_been_submitted()
    {
        await dafda_serialize_deserialize(new NewMembershipInvitationHasBeenSubmitted());
        await dafda_serialize_deserialize(
            new NewMembershipInvitationHasBeenSubmitted { MembershipInvitationId = TestMemberShipId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_invitation_has_been_declined()
    {
        await dafda_serialize_deserialize(new NewMembershipInvitationHasBeenDeclined());
        await dafda_serialize_deserialize(
            new NewMembershipInvitationHasBeenDeclined { MembershipInvitationId = TestMemberShipId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_invitation_has_been_accepted()
    {
        await dafda_serialize_deserialize(new NewMembershipInvitationHasBeenAccepted());
        await dafda_serialize_deserialize(
            new NewMembershipInvitationHasBeenAccepted { MembershipInvitationId = TestMemberShipId }
        );
    }

    [Fact]
    public async Task dafda_serialize_deserialize_membership_invitation_has_been_cancelled()
    {
        await dafda_serialize_deserialize(new NewMembershipInvitationHasBeenCancelled());
        await dafda_serialize_deserialize(
            new NewMembershipInvitationHasBeenCancelled { MembershipInvitationId = TestMemberShipId }
        );
    }
}
