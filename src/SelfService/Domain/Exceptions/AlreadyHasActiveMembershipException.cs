namespace SelfService.Domain.Exceptions;

public class AlreadyHasActiveMembershipException : Exception
{
    public AlreadyHasActiveMembershipException(string message) : base(message)
    {
        
    }
}