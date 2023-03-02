namespace SelfService.Domain.Exceptions;

public class PendingMembershipApplicationAlreadyExistsException : Exception
{
    public PendingMembershipApplicationAlreadyExistsException(string message) : base(message)
    {
        
    }
}