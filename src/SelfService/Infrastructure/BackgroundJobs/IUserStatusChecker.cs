using System.Threading.Tasks;
namespace SelfService.Infrastructure.BackgroundJobs;
public interface IUserStatusChecker
{
    Task<(bool, string)> MakeUserRequest(string userId);
}