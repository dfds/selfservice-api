using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Tests.Comparers;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestReleaseNoteRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task create_inserts_expected_release_note_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stub = A.ReleaseNote;

        var sut = A.ReleaseNoteRepository.WithDbContext(dbContext).Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.ReleaseNotes.ToListAsync());
        Assert.Equal(stub, inserted, new ReleaseNoteComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task get_all_returns_all_release_notes()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = A.ReleaseNoteRepository.WithDbContext(dbContext).Build();
        await sut.Add(A.ReleaseNote.WithTitle("note1").Build());
        await sut.Add(A.ReleaseNote.WithTitle("note2").Build());
        await sut.Add(A.ReleaseNote.WithTitle("note3").Build());

        await dbContext.SaveChangesAsync();
        var allReleaseNotes = await sut.GetAll();
        Assert.Equal(3, allReleaseNotes.Count());
        Assert.Contains(allReleaseNotes, note => note.Title == "note1");
        Assert.Contains(allReleaseNotes, note => note.Title == "note2");
        Assert.Contains(allReleaseNotes, note => note.Title == "note3");
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task get_release_note_returns_expected_release_note()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = A.ReleaseNoteRepository.WithDbContext(dbContext).Build();
        var expected = A.ReleaseNote.Build();
        await sut.Add(expected);
        await dbContext.SaveChangesAsync();
        var actual = await sut.Get(expected.Id);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual, new ReleaseNoteComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task toggle_active_changes_is_active_status()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var sut = A.ReleaseNoteRepository.WithDbContext(dbContext).Build();
        var note = A.ReleaseNote.WithIsActive(true).Build();
        await sut.Add(note);
        await dbContext.SaveChangesAsync();
        var originalNote = await sut.Get(note.Id);
        Assert.NotNull(originalNote);
        Assert.True(originalNote.IsActive);

        await sut.ToggleActive(note.Id);
        var updatedNote = await sut.Get(note.Id);
        Assert.NotNull(updatedNote);
        Assert.False(updatedNote.IsActive);
    }
}
