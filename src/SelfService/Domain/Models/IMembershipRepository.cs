namespace SelfService.Domain.Models;

public interface IMembershipRepository
{
    Task Add(Membership membership);
}