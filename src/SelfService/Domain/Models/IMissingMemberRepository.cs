namespace SelfService.Domain.Models;

public interface IMissingMemberRepository
{
    Task<MissingMemberRecord?> FindByUser(string userId);
    Task Add(MissingMemberRecord record);
    Task Update(MissingMemberRecord record);
    Task RemoveByUserId(string userId);
}
