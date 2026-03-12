using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Infrastructure.Persistence;

public class TemplateRenderingService : ITemplateRenderingService
{
    public string RenderTemplate(string template, Capability capability, Member? member, string campaignName, int memberCount = 0)
    {
        var result = template;

        // Capability variables
        result = result.Replace("{{Capability.Id}}", capability.Id.ToString());
        result = result.Replace("{{Capability.Name}}", capability.Name ?? "");
        result = result.Replace("{{Capability.Description}}", capability.Description ?? "");
        result = result.Replace("{{Capability.Status}}", capability.Status.ToString());
        result = result.Replace("{{Capability.CreatedAt}}", capability.CreatedAt.ToString("yyyy-MM-dd"));
        result = result.Replace("{{Capability.CreatedBy}}", capability.CreatedBy ?? "");
        result = result.Replace("{{Capability.RequirementScore}}",
            capability.RequirementScore?.ToString("0") ?? "N/A");
        result = result.Replace("{{Capability.MemberCount}}", memberCount.ToString());

        // Member variables
        result = result.Replace("{{Member.DisplayName}}", member?.DisplayName ?? "[Member Name]");
        result = result.Replace("{{Member.Email}}", member?.Email ?? "[Member Email]");

        // Campaign variables
        result = result.Replace("{{Campaign.Name}}", campaignName);

        // Date variables
        result = result.Replace("{{Date.Today}}", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        result = result.Replace("{{Date.Year}}", DateTime.UtcNow.Year.ToString());

        // Metadata variables — resolve any remaining {{Metadata.*}} tokens
        result = RenderMetadataVariables(result, capability.JsonMetadata);

        return result;
    }

    private static string RenderMetadataVariables(string template, string? jsonMetadata)
    {
        if (string.IsNullOrEmpty(jsonMetadata) || !template.Contains("{{Metadata."))
            return template;

        try
        {
            using var doc = JsonDocument.Parse(jsonMetadata);
            var result = template;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var token = $"{{{{Metadata.{prop.Name}}}}}";
                var value = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() ?? ""
                    : prop.Value.GetRawText();
                result = result.Replace(token, value);
            }
            return result;
        }
        catch
        {
            return template;
        }
    }
}
