using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IDemoApplicationService
{
    Task<IEnumerable<DemoSignup>> GetActiveSignups();
}
