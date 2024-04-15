using PChecker.Actors;
using PChecker.Actors.Logging;

public interface ISendEventMonitor {
    public void OnSendEvent(ActorId sender, int loc, ActorId receiver, VectorClockGenerator currentVc);

    public void OnSendEventDone(ActorId sender, int loc, ActorId receiver, VectorClockGenerator currentVc);
}