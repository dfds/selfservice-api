using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class DemoApplicationService : IDemoApplicationService
{
    private readonly IMemberRepository _memberRepository;

    public DemoApplicationService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<IEnumerable<DemoSignup>> GetActiveSignups()
    {
        var members = await _memberRepository.GetAll();
        return members
            .Where(m => m.UserSettings.SignedUpForDemos == true)
            .Select(m => new DemoSignup(email: m.Email, name: m.DisplayName ?? ""));
    }
}
