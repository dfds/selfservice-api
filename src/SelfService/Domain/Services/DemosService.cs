using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Domain.Services;

public class DemosService : IDemosService
{
    private readonly ILogger<DemosService> _logger;
    private readonly IDemosRepository _demosRepository;

    public DemosService(ILogger<DemosService> logger, IDemosRepository demosRepository)
    {
        _logger = logger;
        _demosRepository = demosRepository;
    }

    public async Task<IEnumerable<Demo>> GetAllDemos()
    {
        return await _demosRepository.GetAll();
    }

    public async Task<Demo> GetDemoById(DemoId demoId)
    {
        var demo =
            await _demosRepository.FindById(demoId)
            ?? throw new KeyNotFoundException($"Demo with id '{demoId}' not found.");
        return demo;
    }

    [TransactionalBoundary]
    public async Task<Demo> AddDemo(Demo demo)
    {
        await _demosRepository.Add(demo);
        return demo;
    }

    [TransactionalBoundary]
    public async Task UpdateDemo(DemoId demoId, DemoUpdateRequest updateRequest)
    {
        var demo =
            await _demosRepository.FindById(demoId)
            ?? throw new KeyNotFoundException($"Demo with id '{demoId}' not found.");

        demo.Update(updateRequest);
    }

    [TransactionalBoundary]
    public async Task DeleteDemo(DemoId demoId)
    {
        await _demosRepository.Remove(demoId);
    }
}
