namespace SelfService.Domain.Services;
public interface IUserStatusChecker
{
    Task<(bool, string)> MakeUserRequest(string userId);
}