namespace SelfService.Infrastructure.Api.Capabilities;

public class MembershipApprovalApiResource
{
    public string Id { get; set; }
    public string ApprovedBy { get; set; }
    public string ApprovedAt { get; set; }

    public MembershipApprovalApiResource(string id, string approvedBy, string approvedAt)
    {
        Id = id;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }
}
