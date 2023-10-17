﻿using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
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
        public ResourceLink RequestCapabilityDeletion { get; set; }
        public ResourceLink CancelCapabilityDeletionRequest { get; set; }
        public ResourceLink SetCapabilityMetadata { get; set; }
        public ResourceLink GetCapabilityMetadata { get; set; }
        public ResourceLink GetLinkedTeams { get; set; }

        public CapabilityDetailsLinks(
            ResourceLink self,
            ResourceLink members,
            ResourceLink clusters,
            ResourceLink membershipApplications,
            ResourceLink leaveCapability,
            ResourceLink awsAccount,
            ResourceLink requestCapabilityDeletion,
            ResourceLink cancelCapabilityDeletionRequest,
            ResourceLink setCapabilityMetadata,
            ResourceLink getCapabilityMetadata,
            ResourceLink getLinkedTeams
        )
        {
            Self = self;
            Members = members;
            Clusters = clusters;
            MembershipApplications = membershipApplications;
            LeaveCapability = leaveCapability;
            AwsAccount = awsAccount;
            RequestCapabilityDeletion = requestCapabilityDeletion;
            CancelCapabilityDeletionRequest = cancelCapabilityDeletionRequest;
            SetCapabilityMetadata = setCapabilityMetadata;
            GetCapabilityMetadata = getCapabilityMetadata;
            GetLinkedTeams = getLinkedTeams;
        }
    }

    public CapabilityDetailsApiResource(
        string id,
        string name,
        string status,
        string description,
        string jsonMetadata,
        int jsonMetadataSchemaVersion,
        CapabilityDetailsLinks links
    )
    {
        Id = id;
        Name = name;
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
    public string Status { get; set; }
    public string Description { get; set; }

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
        string status,
        string description,
        CapabilityListItemLinks links
    )
    {
        Id = id;
        Name = name;
        Status = status;
        Description = description;
        Links = links;
    }
}
