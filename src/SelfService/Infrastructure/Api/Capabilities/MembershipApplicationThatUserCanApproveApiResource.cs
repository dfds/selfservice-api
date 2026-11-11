using System.Text.Json.Serialization;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationThatUserCanApproveApiResource : MembershipApplicationApiResource
{
    public string CapabilityId { get; set; }

    /// <summary>Roles the caller may grant to the applicant when approving.</summary>
    public List<RbacRoleDTO> GrantableRoles { get; set; } = new();

    public MembershipApplicationThatUserCanApproveApiResource(
        string id,
        string capabilityId,
        string applicant,
        string submittedAt,
        string expiresOn,
        MembershipApprovalListApiResource approvals,
        MembershipApplicationLinks links
    )
        : base(id, applicant, submittedAt, expiresOn, approvals, links)
    {
        CapabilityId = capabilityId;
    }
}
