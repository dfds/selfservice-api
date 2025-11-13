using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class DemoRecordingRepository : GenericRepository<DemoRecording, DemoRecordingId>, IDemoRecordingRepository
{
    public DemoRecordingRepository(SelfServiceDbContext dbContext)
        : base(dbContext.DemoRecording) { }
}
