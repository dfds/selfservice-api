namespace SelfService.Domain.Exceptions;

public class MembershipAlreadyFinalisedException : Exception
{
    public MembershipAlreadyFinalisedException(string message)
        : base(message) { }
}
