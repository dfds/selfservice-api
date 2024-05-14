namespace SelfService.Domain.Exceptions;

public class AlreadyHasAzureResourceException : Exception
{
    public AlreadyHasAzureResourceException(string message)
        : base(message) { }
}
