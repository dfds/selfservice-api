using SelfService.Domain.Models;

namespace SelfService.Domain.Exceptions;

public class InvalidJsonMetadataException : Exception
{
    public InvalidJsonMetadataException(ValidateJsonMetadataResult result)
        : base($"Invalid Json Metadata, errors: {result.Error}") { }
}
