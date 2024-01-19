namespace SelfService.Domain.Exceptions;

public class InvalidMessageContractRequestException : Exception
{
    public InvalidMessageContractRequestException(string message)
        : base($"Invalid message contract request: {message}") { }
}
