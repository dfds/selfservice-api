using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record AwsAccountDto(Guid Id, string? AccountId, string? RoleArn, string? RoleEmail)
{
    public static AwsAccountDto Create(AwsAccount? context)
    {
        if (context==null)
        {
            return null;
        }
        
        return new AwsAccountDto(
            context.Id,
            context.AccountId,
            context.RoleArn,
            context.RoleEmail
        );
    }
}