using SelfService.Legacy.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record ContextDto(Guid Id, string? Name, string? AWSAccountId, string? AWSRoleArn, string? AWSRoleEmail)
{
    public static ContextDto Create(Context context)
    {
        return new ContextDto(
            context.Id,
            context.Name,
            context.AWSAccountId,
            context.AWSRoleArn,
            context.AWSRoleEmail
        );
    }
}