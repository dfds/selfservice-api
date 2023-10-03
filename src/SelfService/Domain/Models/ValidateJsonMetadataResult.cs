namespace SelfService.Domain.Models;

public enum ValidateJsonMetadataResultCode
{
    SuccessNoSchema,
    SuccessValidJsonMetadata,
    SuccessSchemaHasNoRequiredFields,
    Error
}

public class ValidateJsonMetadataResult
{
    public string? JsonMetadata { get; set; }
    public int JsonSchemaVersion { get; set; }
    public string? Error { get; set; }
    public ValidateJsonMetadataResultCode ResultCode { get; set; }

    public bool IsValid()
    {
        return JsonMetadata != null;
    }

    public string GetErrorString()
    {
        return Error == null ? "" : Error;
    }

    public static ValidateJsonMetadataResult CreateError(string error)
    {
        return new ValidateJsonMetadataResult { Error = error, ResultCode = ValidateJsonMetadataResultCode.Error };
    }

    public static ValidateJsonMetadataResult CreateSuccess(
        string jsonMetadata,
        int jsonSchemaVersion,
        ValidateJsonMetadataResultCode resultCode
    )
    {
        return new ValidateJsonMetadataResult
        {
            JsonMetadata = jsonMetadata,
            JsonSchemaVersion = jsonSchemaVersion,
            ResultCode = resultCode
        };
    }
}
