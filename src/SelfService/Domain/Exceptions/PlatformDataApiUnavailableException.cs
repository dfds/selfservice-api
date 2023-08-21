namespace SelfService.Domain.Exceptions;

public class PlatformDataApiUnavailableException : Exception
{
    public PlatformDataApiUnavailableException(string message)
        : base(message) { }
}
