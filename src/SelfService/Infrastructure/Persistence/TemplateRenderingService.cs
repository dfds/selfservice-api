using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Infrastructure.Persistence;

public class TemplateRenderingService : ITemplateRenderingService
{
    public string RenderTemplate(string template, TemplateRenderContext context)
    {
        var capability = context.Capability;
        var member = context.Member;
        var result = template;

        // Capability variables
        result = result.Replace("{{Capability.Id}}", capability.Id.ToString());
        result = result.Replace("{{Capability.Name}}", capability.Name ?? "");
        result = result.Replace("{{Capability.Description}}", capability.Description ?? "");
        result = result.Replace("{{Capability.Status}}", capability.Status.ToString());
        result = result.Replace("{{Capability.CreatedAt}}", capability.CreatedAt.ToString("yyyy-MM-dd"));
        result = result.Replace("{{Capability.CreatedBy}}", capability.CreatedBy ?? "");
        result = result.Replace("{{Capability.RequirementScore}}", capability.RequirementScore?.ToString("0") ?? "N/A");
        result = result.Replace("{{Capability.MemberCount}}", context.MemberCount.ToString());

        // Member variables
        result = result.Replace("{{Member.DisplayName}}", member?.DisplayName ?? "[Member Name]");
        result = result.Replace("{{Member.Email}}", member?.Email ?? "[Member Email]");

        // Campaign variables
        result = result.Replace("{{Campaign.Name}}", context.CampaignName);

        // Date variables
        result = result.Replace("{{Date.Today}}", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        result = result.Replace("{{Date.Year}}", DateTime.UtcNow.Year.ToString());

        // AWS account variables
        var aws = context.AwsAccount;
        result = result.Replace("{{Aws.AccountId}}", aws?.Registration.AccountId?.ToString() ?? "N/A");
        result = result.Replace("{{Aws.Status}}", aws?.Status.ToString() ?? "N/A");
        result = result.Replace("{{Aws.Namespace}}", aws?.KubernetesLink.Namespace ?? "N/A");
        result = result.Replace("{{Aws.RoleEmail}}", aws?.Registration.RoleEmail ?? "N/A");

        // Azure resource variables
        result = RenderAzureVariables(result, context.AzureResources);

        // Membership application variables
        result = result.Replace(
            "{{MembershipApplications.PendingCount}}",
            context.PendingMembershipApplicationCount.ToString()
        );

        // Dynamic variables
        result = RenderMetadataVariables(result, capability.JsonMetadata);
        result = RenderRequirementVariables(result, context.RequirementScores);

        return result;
    }

    private static string RenderAzureVariables(string template, List<AzureResource> azureResources)
    {
        var result = template;

        result = result.Replace("{{Azure.ResourceCount}}", azureResources.Count.ToString());
        result = result.Replace(
            "{{Azure.Environments}}",
            azureResources.Count > 0
                ? string.Join(", ", azureResources.Select(r => r.Environment).OrderBy(e => e))
                : "None"
        );

        if (!result.Contains("{{Azure."))
            return result;

        // Dynamic per-environment variables: {{Azure.<environment>.Id}}
        foreach (var resource in azureResources)
        {
            result = result.Replace($"{{{{Azure.{resource.Environment}.Id}}}}", resource.Id.ToString());
        }

        return result;
    }

    private static string RenderRequirementVariables(string template, List<RequirementsMetric> scores)
    {
        if (!template.Contains("{{Requirement."))
            return template;

        var result = template;
        foreach (var metric in scores)
        {
            result = result.Replace($"{{{{Requirement.{metric.RequirementId}}}}}", metric.Value.ToString("0"));
            result = result.Replace(
                $"{{{{Requirement.{metric.RequirementId}.DisplayName}}}}",
                metric.DisplayName ?? metric.RequirementId
            );
            result = result.Replace($"{{{{Requirement.{metric.RequirementId}.HelpUrl}}}}", metric.HelpUrl ?? "");
        }

        // Replace any remaining Requirement variables that didn't match a metric
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\{Requirement\.[^}]+\}\}", "N/A");

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
                var value =
                    prop.Value.ValueKind == JsonValueKind.String
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
