namespace SelfService.Domain.Exceptions;

public class InvalidMessageContractEnvelopeException : Exception
{
    public InvalidMessageContractEnvelopeException(string message)
        : base($"Invalid message contract envelope: {message}") { }
}
