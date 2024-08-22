using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApplicationThatUserCanApproveApiResource : MembershipApplicationApiResource
{
    public string CapabilityId { get; set; }

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
