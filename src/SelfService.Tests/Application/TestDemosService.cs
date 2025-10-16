using Json.Schema;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Tests.Application;

public class TestDemosService
{
    [Fact]
    public async Task add_demo()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demosRepository = A.DemosRepository.WithDbContext(dbContext).Build();
        var demosService = A.DemosService.WithDemosRepository(demosRepository).Build();

        var demo = A.Demo.Build();
        await demosRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var allDemos = await demosService.GetAllDemos();
        Assert.Equal(demo.Id, allDemos.First().Id);
    }

    [Fact]
    public async Task delete_demo()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demosRepository = A.DemosRepository.WithDbContext(dbContext).Build();
        var demosService = A.DemosService.WithDemosRepository(demosRepository).Build();

        var demo = A.Demo.Build();
        await demosRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var allDemos = await demosService.GetAllDemos();
        Assert.Equal(demo.Id, allDemos.First().Id);

        await demosService.DeleteDemo(demo.Id);
        await dbContext.SaveChangesAsync();

        var allDemosAfterDelete = await demosService.GetAllDemos();
        Assert.Empty(allDemosAfterDelete);
    }

    [Fact]
    public async Task get_specific_demo()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demosRepository = A.DemosRepository.WithDbContext(dbContext).Build();
        var demosService = A.DemosService.WithDemosRepository(demosRepository).Build();

        var demo1 = A.Demo.Build();
        await demosRepository.Add(demo1);

        var demo2 = A.Demo.Build();
        await demosRepository.Add(demo2);

        await dbContext.SaveChangesAsync();

        var fetchedDemo = await demosService.GetDemoById(demo2.Id);
        Assert.Equal(demo2.Id, fetchedDemo.Id);
    }

    [Fact]
    public async Task update_demo()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demosRepository = A.DemosRepository.WithDbContext(dbContext).Build();
        var demosService = A.DemosService.WithDemosRepository(demosRepository).Build();

        var demo = A.Demo.WithTitle("Initial Demo").Build();
        await demosRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var fetchedDemo = await demosService.GetDemoById(demo.Id);
        Assert.Equal("Initial Demo", fetchedDemo.Title);

        var demoUpdate = new DemoUpdateRequest();
        demoUpdate.Title = "Updated Demo";

        demo.Update(demoUpdate);
        await dbContext.SaveChangesAsync();

        var fetchedUpdatedDemo = await demosService.GetDemoById(demo.Id);
        Assert.Equal("Updated Demo", fetchedUpdatedDemo.Title);
    }
}
