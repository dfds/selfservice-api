namespace SelfService.Domain.Exceptions;

public class AwsAccountLimitExceededException : Exception
{
    public AwsAccountLimitExceededException(string message)
        : base(message) { }
}
