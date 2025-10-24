using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Services;

public interface IDemosService
{
    Task<IEnumerable<Demo>> GetAllDemos();
    Task<Demo> GetDemoById(DemoId demoId);
    Task<Demo> AddDemo(Demo createRequest);
    Task UpdateDemo(DemoId demoId, DemoUpdateRequest updateRequest);
    Task DeleteDemo(DemoId demoId);
}
