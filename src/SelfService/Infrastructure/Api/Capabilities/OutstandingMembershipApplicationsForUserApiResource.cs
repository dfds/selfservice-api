namespace SelfService.Infrastructure.Api.Capabilities;

public class OutstandingMembershipApplicationsForUserApiResource : MembershipApplicationApiResource
{
    public string CapabilityId { get; set; }

    public OutstandingMembershipApplicationsForUserApiResource(
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
