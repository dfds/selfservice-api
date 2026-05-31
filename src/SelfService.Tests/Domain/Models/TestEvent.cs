using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestEvent
{
    private static Event NewEvent(DateTime eventDate) =>
        new(
            id: new EventId(),
            eventDate: eventDate,
            title: "title",
            description: "desc",
            type: EventType.Demo,
            createdBy: "tester",
            createdAt: DateTime.UtcNow
        );

    [Fact]
    public void event_date_with_unspecified_kind_is_stored_as_utc()
    {
        var input = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Unspecified);

        var ev = NewEvent(input);

        Assert.Equal(DateTimeKind.Utc, ev.EventDate.Kind);
        Assert.Equal(new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Utc), ev.EventDate);
    }

    [Fact]
    public void event_date_with_local_kind_is_converted_to_utc()
    {
        var input = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Local);

        var ev = NewEvent(input);

        Assert.Equal(DateTimeKind.Utc, ev.EventDate.Kind);
        Assert.Equal(input.ToUniversalTime(), ev.EventDate);
    }

    [Fact]
    public void event_date_with_utc_kind_is_preserved()
    {
        var input = new DateTime(2026, 6, 15, 14, 30, 0, DateTimeKind.Utc);

        var ev = NewEvent(input);

        Assert.Equal(input, ev.EventDate);
        Assert.Equal(DateTimeKind.Utc, ev.EventDate.Kind);
    }

    [Fact]
    public void update_normalises_event_date_to_utc()
    {
        var ev = NewEvent(new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc));
        var newDate = new DateTime(2026, 7, 1, 16, 30, 0, DateTimeKind.Unspecified);

        ev.Update(eventDate: newDate, title: null, description: null, type: null);

        Assert.Equal(DateTimeKind.Utc, ev.EventDate.Kind);
        Assert.Equal(new DateTime(2026, 7, 1, 16, 30, 0, DateTimeKind.Utc), ev.EventDate);
    }

    [Fact]
    public void is_upcoming_uses_full_instant_including_time()
    {
        var inFiveMinutes = DateTime.UtcNow.AddMinutes(5);
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

        Assert.True(NewEvent(inFiveMinutes).IsUpcoming());
        Assert.False(NewEvent(fiveMinutesAgo).IsUpcoming());
    }
}
