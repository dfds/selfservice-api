namespace SelfService.Domain.Models;

public interface IMemberRepository
{
    Task Add(Member member);
    Task<Member?> FindBy(UserId userId);
    Task<List<Member>> GetAll();

    Task<Member> Remove(UserId userId);
}
