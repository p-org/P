using PChecker.Actors.Events;
using PChecker.Specifications.Monitors;

namespace PChecker.Matcher;

public class EventObj
{
    public Event Event;
    public string? Sender;
    public string? Receiver;
    public string State;
    public int Index;

    public EventObj(Event e, string? sender, string? receiver, string state, int index)
    {
        Event = e;
        Sender = sender;
        Receiver = receiver;
        State = state;
        Index = index;
    }
}