using Microsoft.AspNetCore.Mvc;
using SelfService.Infrastructure.Api.Configuration;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Infrastructure.Api.System;

public static class Module
{
    public static void MapSystemModule(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => "Hello World!").WithTags("System").NoSwaggerDocs();
    }
}

[Route("system")]
[ApiController]
public class SystemController : ControllerBase
{
    private readonly TopVisitorsRepository _topVisitorsRepository;

    public SystemController(TopVisitorsRepository topVisitorsRepository)
    {
        _topVisitorsRepository = topVisitorsRepository;
    }

    [Route("stats/topvisitors")]
    public async Task<IActionResult> GetTopVisitors()
    {
        var visitorRecords = _topVisitorsRepository.GetAll();
        return Ok(new
        {
            Items = visitorRecords
                .OrderBy(x => x.Rank)
                .Select(x => new
            {
                Id = x.Id.ToString(),
                Name = x.Name, 
                Rank = x.Rank,
            }),

        });
    }
}
