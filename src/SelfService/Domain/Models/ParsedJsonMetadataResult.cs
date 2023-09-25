namespace SelfService.Domain.Models;

public enum ParsedJsonMetadataResultCode
{
    SuccessNoSchema,
    SuccessValidJsonMetadata,
    SuccessSchemaHasNoRequiredFields,
    Error
}

public class ParsedJsonMetadataResult
{
    public string? JsonMetadata { get; set; }
    public int JsonSchemaVersion { get; set; }
    public string? Error { get; set; }
    public ParsedJsonMetadataResultCode ResultCode { get; set; }

    public bool IsValid()
    {
        return JsonMetadata != null;
    }

    public string GetErrorString()
    {
        return Error == null ? "" : Error;
    }

    public static ParsedJsonMetadataResult CreateError(string error)
    {
        return new ParsedJsonMetadataResult { Error = error, ResultCode = ParsedJsonMetadataResultCode.Error };
    }

    public static ParsedJsonMetadataResult CreateSuccess(
        string jsonMetadata,
        int jsonSchemaVersion,
        ParsedJsonMetadataResultCode resultCode
    )
    {
        return new ParsedJsonMetadataResult
        {
            JsonMetadata = jsonMetadata,
            JsonSchemaVersion = jsonSchemaVersion,
            ResultCode = resultCode
        };
    }
}
