namespace SelfService.Domain.Exceptions;

public class DataPlatformApiUnavailableException : Exception
{
    public DataPlatformApiUnavailableException(string message):base(message)
    {
        
    }
}