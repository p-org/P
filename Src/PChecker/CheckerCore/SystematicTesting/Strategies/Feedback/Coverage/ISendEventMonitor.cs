using PChecker.Actors;
using PChecker.Actors.Logging;

public record Operation(string Sender, string Receiver, int Loc) {
    public override string ToString()
    {
        return $"<{Sender}, {Receiver}, {Loc}>";
    }

}
public interface ISendEventMonitor {
    public void OnSendEvent(ActorId sender, int loc, ActorId receiver, VectorClockGenerator currentVc);

    public void OnSendEventDone(ActorId sender, int loc, ActorId receiver, VectorClockGenerator currentVc);
}