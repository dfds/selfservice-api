using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface ITemplateRenderingService
{
    string RenderTemplate(string template, TemplateRenderContext context);

    IReadOnlyList<TemplateVariable> GetVariableDefinitions(EmailCampaignTargetType? targetType = null);
}
