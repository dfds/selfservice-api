using System.Threading.Tasks;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.BackgroundJobs;

public interface IUserStatusChecker
{
    Task<UserStatusCheckerStatus> CheckUserStatus(string userId);
    Task<bool> TrySetAuthToken();
}
