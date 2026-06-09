using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

/// <summary>
/// Request body for batch capability creation.
/// </summary>
public class BatchCapabilityRequest
{
    /// <summary>List of proto-capabilities to create.</summary>
    [Required]
    public List<ProtoCapabilityRequest> ProtoCapabilities { get; set; } = new();
}

/// <summary>
/// Describes a single capability to create as part of a batch operation.
/// </summary>
public class ProtoCapabilityRequest
{
    /// <summary>The name of the capability. Used to derive the capability ID.</summary>
    [Required]
    public string? Name { get; set; }

    /// <summary>Optional description of the capability.</summary>
    public string? Description { get; set; }

    /// <summary>The @dfds.com email address of the capability owner. Will be granted the Owner role.</summary>
    [Required]
    public string? Owner { get; set; }

    /// <summary>
    /// @dfds.com email addresses to add as members. At least one is required.
    /// The owner is always added as a member regardless of whether they appear here.
    /// </summary>
    public List<string> Members { get; set; } = new();

    /// <summary>
    /// Metadata tags for the capability. Must include 'dfds.cost.centre'.
    /// If AzureResourceGroups are specified, must also include 'dfds.service.availability',
    /// 'dfds.azure.purpose', and 'dfds.service.capabilities'.
    /// </summary>
    [Required]
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>Azure resource groups to request for this capability. Optional.</summary>
    public List<ProtoAzureResourceGroupRequest> AzureResourceGroups { get; set; } = new();
}

/// <summary>Describes an Azure resource group to request for a capability.</summary>
public class ProtoAzureResourceGroupRequest
{
    /// <summary>The target environment (e.g. "prod", "dev").</summary>
    [Required]
    public string? Environment { get; set; }

    /// <summary>
    /// The purpose of the resource group. Must be one of the legal azure purpose values.
    /// When purpose is 'ai', CatalogueId, Risk, and Gdpr are all required.
    /// </summary>
    [Required]
    public string? Purpose { get; set; }

    /// <summary>Required when Purpose is 'ai'.</summary>
    public string? CatalogueId { get; set; }

    /// <summary>Required when Purpose is 'ai'.</summary>
    public string? Risk { get; set; }

    /// <summary>Required when Purpose is 'ai'.</summary>
    public bool? Gdpr { get; set; }
}
