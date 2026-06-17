namespace SelfService.Infrastructure.Api.System;

public interface IAadAwsSyncCapabilityQuery
{
    Task<IEnumerable<CapabilityDto>> GetCapabilities();
}

public class CapabilityDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string RootId { get; set; }
    public required string Description { get; set; }
    public required MemberDto[] Members { get; set; }
    public required ContextDto[] Contexts { get; set; }
    public required string JsonMetadata { get; set; }
}

public class MemberDto
{
    public required string Email { get; set; }

    // The member's identifier. For regular users this is their UPN, which lets
    // aad-aws-sync look the user up in Azure AD directly instead of guessing via
    // email — correct even when the user's UPN differs from their email address.
    public required string UserId { get; set; }
}

public class ContextDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string? AWSAccountId { get; set; }
    public required string? AWSRoleArn { get; set; }
    public required string? AWSRoleEmail { get; set; }
}
