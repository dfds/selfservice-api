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
}

public class MemberDto
{
    public required string Email { get; set; }
}

public class ContextDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string? AWSAccountId { get; set; }
    public required string? AWSRoleArn { get; set; }
    public required string? AWSRoleEmail { get; set; }
}
