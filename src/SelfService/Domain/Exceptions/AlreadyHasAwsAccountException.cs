namespace SelfService.Domain.Exceptions;

public class AlreadyHasAwsAccountException : Exception
{
    public AlreadyHasAwsAccountException(string message)
        : base(message) { }
}
