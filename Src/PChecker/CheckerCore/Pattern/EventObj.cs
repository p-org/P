using PChecker.Actors.Events;

namespace PChecker.Matcher;

public class EventObj
{
    public Event Event;
    public string? Sender;
    public string? Receiver;
    public int Index;

    public EventObj(Event e, string? sender, string? receiver, int index)
    {
        Event = e;
        Sender = sender;
        Receiver = receiver;
        Index = index;
    }
}