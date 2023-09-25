﻿using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }

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

        public CapabilityDetailsLinks(
            ResourceLink self,
            ResourceLink members,
            ResourceLink clusters,
            ResourceLink membershipApplications,
            ResourceLink leaveCapability,
            ResourceLink awsAccount,
            ResourceLink requestCapabilityDeletion,
            ResourceLink cancelCapabilityDeletionRequest
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
