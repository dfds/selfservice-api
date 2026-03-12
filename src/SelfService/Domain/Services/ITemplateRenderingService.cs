using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface ITemplateRenderingService
{
    string RenderTemplate(string template, Capability capability, Member? member, string campaignName, int memberCount = 0);
}
