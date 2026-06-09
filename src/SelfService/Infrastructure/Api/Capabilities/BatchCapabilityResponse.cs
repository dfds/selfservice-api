namespace SelfService.Infrastructure.Api.Capabilities;

/// <summary>Result of a batch capability creation request.</summary>
public class BatchCapabilityResponse
{
    /// <summary>Capabilities that were successfully created.</summary>
    public List<CreatedCapabilityResult> Created { get; set; } = new();

    /// <summary>Proto-capabilities that failed validation or could not be created, with error details.</summary>
    public List<FailedCapabilityResult> Failed { get; set; } = new();
}

/// <summary>A capability that was successfully created.</summary>
public class CreatedCapabilityResult
{
    public string CapabilityId { get; set; } = "";
    public string Name { get; set; } = "";
}

/// <summary>A proto-capability that could not be created.</summary>
public class FailedCapabilityResult
{
    public string Name { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}
