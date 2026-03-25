namespace SelfService.Domain.Services;

public interface ITemplateRenderingService
{
    string RenderTemplate(string template, TemplateRenderContext context);
}
