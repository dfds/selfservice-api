namespace SelfService.Domain.Exceptions;

public class MissingMandatoryJsonMetadataException : Exception
{
    public MissingMandatoryJsonMetadataException(string message)
        : base(message) { }
}
