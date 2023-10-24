using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Application;

public interface IDeactivatedMemberCleanerApplicationService
{
    Task RemoveDeactivatedMemberships(IUserStatusChecker userStatusChecker);
}