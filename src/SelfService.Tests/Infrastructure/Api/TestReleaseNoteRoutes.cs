
using System.Text.Json;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Api;

public class TestReleaseNoteRoutes
{
    [Fact]
    public async Task get_release_note_by_id_returns_expected_permissions_on_toggle_active_link()
    {
        var stubReleaseNote = A.ReleaseNote.Build();

        await using var application = new ApiApplicationBuilder()
            .WithReleaseNoteRepository(new StubReleaseNoteRepository(stubReleaseNote))
            .Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/release-notes/{stubReleaseNote.Id}");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/toggleIsActive/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }

    [Fact]
    public async Task get_release_note_list_returns_expected_permissions_on_create_link()
    {
        await using var application = new ApiApplicationBuilder().Build();

        using var client = application.CreateClient();
        var response = await client.GetAsync($"/release-notes");

        var content = await response.Content.ReadAsStringAsync();
        var document = JsonSerializer.Deserialize<JsonDocument>(content);

        var allowValues = document
            ?.SelectElement("/_links/createReleaseNote/allow")
            ?.EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .ToArray();

        Assert.Empty(allowValues!);
    }
}
