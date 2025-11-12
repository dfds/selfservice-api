using Json.Schema;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Demos;

namespace SelfService.Tests.Application;

public class TestDemoRecordingService
{
    [Fact]
    public async Task add_recording()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demoRecordingRepository = A.DemoRecordingRepository.WithDbContext(dbContext).Build();
        var demoRecordingService = A.DemoRecordingService.WithDemoRecordingRepository(demoRecordingRepository).Build();

        var demo = A.DemoRecording.Build();
        await demoRecordingRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var allDemos = await demoRecordingService.GetAllDemoRecordings();
        Assert.Equal(demo.Id, allDemos.First().Id);
    }

    [Fact]
    public async Task delete_recording()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demoRecordingsRepository = A.DemoRecordingRepository.WithDbContext(dbContext).Build();
        var demoRecordingsService = A.DemoRecordingService.WithDemoRecordingRepository(demoRecordingsRepository).Build();

        var demo = A.DemoRecording.Build();
        await demoRecordingsRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var allDemos = await demoRecordingsService.GetAllDemoRecordings();
        Assert.Equal(demo.Id, allDemos.First().Id);

        await demoRecordingsService.DeleteDemoRecording(demo.Id);
        await dbContext.SaveChangesAsync();

        var allDemosAfterDelete = await demoRecordingsService.GetAllDemoRecordings();
        Assert.Empty(allDemosAfterDelete);
    }

    [Fact]
    public async Task get_specific_recording()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demoRecordingRepository = A.DemoRecordingRepository.WithDbContext(dbContext).Build();
        var demoRecordingService = A.DemoRecordingService.WithDemoRecordingRepository(demoRecordingRepository).Build();

        var demo1 = A.DemoRecording.Build();
        await demoRecordingRepository.Add(demo1);
        var demo2 = A.DemoRecording.Build();
        await demoRecordingRepository.Add(demo2);

        await dbContext.SaveChangesAsync();

        var fetchedDemo = await demoRecordingService.GetDemoRecordingById(demo2.Id);
        Assert.Equal(demo2.Id, fetchedDemo.Id);
    }

    [Fact]
    public async Task update_recording()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var demoRecordingRepository = A.DemoRecordingRepository.WithDbContext(dbContext).Build();
        var demoRecordingService = A.DemoRecordingService.WithDemoRecordingRepository(demoRecordingRepository).Build();

        var demo = A.DemoRecording.WithTitle("Initial Demo").Build();
        await demoRecordingRepository.Add(demo);
        await dbContext.SaveChangesAsync();

        var fetchedDemo = await demoRecordingService.GetDemoRecordingById(demo.Id);
        Assert.Equal("Initial Demo", fetchedDemo.Title);

        var demoUpdate = new DemoRecordingUpdateRequest();
        demoUpdate.Title = "Updated Demo";

        demo.Update(demoUpdate);
        await dbContext.SaveChangesAsync();

        var fetchedUpdatedDemo = await demoRecordingService.GetDemoRecordingById(demo.Id);
        Assert.Equal("Updated Demo", fetchedUpdatedDemo.Title);
    }
}
