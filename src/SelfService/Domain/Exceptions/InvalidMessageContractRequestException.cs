namespace SelfService.Domain.Exceptions;

public class InvalidMessageContractRequestException : Exception
{
    public string ValidationError { get; set; }

    public InvalidMessageContractRequestException(string message)
        : base($"Invalid message contract request: {message}")
    {
        ValidationError = message;
    }
}
