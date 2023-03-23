namespace SelfService.Domain.Exceptions;

public class NotAuthorizedToApproveMembershipApplication : Exception
{
    public NotAuthorizedToApproveMembershipApplication(string message) : base(message)
    {
    }
}