using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }

    public string JsonMetadata { get; set; }
    public int JsonMetadataSchemaVersion { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityDetailsLinks Links { get; set; }

    public class CapabilityDetailsLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink Members { get; set; }
        public ResourceLink Clusters { get; set; }
        public ResourceLink MembershipApplications { get; set; }
        public ResourceLink LeaveCapability { get; set; }
        public ResourceLink AwsAccount { get; set; }
        public ResourceLink AzureResources { get; set; }
        public ResourceLink RequestCapabilityDeletion { get; set; }
        public ResourceLink CancelCapabilityDeletionRequest { get; set; }

        public ResourceLink Metadata { get; set; }
        public ResourceLink SetRequiredMetadata { get; set; }
        public ResourceLink GetLinkedTeams { get; set; }
        public ResourceLink JoinCapability { get; set; }
        public ResourceLink SendInvitations { get; set; }
        public ResourceLink ConfigurationLevel { get; set; }

        public CapabilityDetailsLinks(
            ResourceLink self,
            ResourceLink members,
            ResourceLink clusters,
            ResourceLink membershipApplications,
            ResourceLink leaveCapability,
            ResourceLink awsAccount,
            ResourceLink azureResources,
            ResourceLink requestCapabilityDeletion,
            ResourceLink cancelCapabilityDeletionRequest,
            ResourceLink metadata,
            ResourceLink setRequiredMetadata,
            ResourceLink getLinkedTeams,
            ResourceLink joinCapability,
            ResourceLink sendInvitations,
            ResourceLink configurationLevel
        )
        {
            Self = self;
            Members = members;
            Clusters = clusters;
            MembershipApplications = membershipApplications;
            LeaveCapability = leaveCapability;
            AwsAccount = awsAccount;
            AzureResources = azureResources;
            RequestCapabilityDeletion = requestCapabilityDeletion;
            CancelCapabilityDeletionRequest = cancelCapabilityDeletionRequest;
            Metadata = metadata;
            SetRequiredMetadata = setRequiredMetadata;
            GetLinkedTeams = getLinkedTeams;
            JoinCapability = joinCapability;
            SendInvitations = sendInvitations;
            ConfigurationLevel = configurationLevel;
        }
    }

    public CapabilityDetailsApiResource(
        string id,
        string name,
        DateTime createdAt,
        string createdBy,
        string status,
        string description,
        string jsonMetadata,
        int jsonMetadataSchemaVersion,
        CapabilityDetailsLinks links
    )
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        Status = status;
        Description = description;
        Links = links;
        JsonMetadata = jsonMetadata;
        JsonMetadataSchemaVersion = jsonMetadataSchemaVersion;
    }
}

public class CapabilityListItemApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public string JsonMetadata { get; set; }
    public string AwsAccountId { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityListItemLinks Links { get; set; }

    public class CapabilityListItemLinks
    {
        public ResourceLink Self { get; set; }

        public CapabilityListItemLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public CapabilityListItemApiResource(
        string id,
        string name,
        DateTime createdAt,
        string createdBy,
        string status,
        string description,
        string jsonMetadata,
        string awsAccountId,
        CapabilityListItemLinks links
    )
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        Status = status;
        Description = description;
        JsonMetadata = jsonMetadata;
        AwsAccountId = awsAccountId;
        Links = links;
    }
}
