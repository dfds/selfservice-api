namespace SelfService.Domain.Services;

public class TemplateVariable
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Example { get; set; } = "";

    /// <summary>
    /// "perCapability" (resolves to a capability — directly in Capability campaigns, only inside
    /// {{#each User.Capabilities}} in User campaigns) or "topLevel" (resolves at the recipient/campaign root).
    /// </summary>
    public string Scope { get; set; } = "";
}
