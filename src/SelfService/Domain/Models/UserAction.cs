namespace SelfService.Domain.Models;

public class UserAction : AggregateRoot<Guid>
{
    public static void NewEvent(Events.UserAction evt)
    {
        var obj = new UserAction();
        obj.Raise(evt);
    }
}
