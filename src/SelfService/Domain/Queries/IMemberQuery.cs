using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMemberQuery
{
    Task<(List<Member> Items, int Total)> Search(MemberType? type, string? search, int limit, int offset);
}
