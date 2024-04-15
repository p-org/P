namespace PChecker.Feedback;

public record Operation(string Sender, string Receiver, int Loc) {
    public override string ToString()
    {
        return $"<{Sender}, {Receiver}, {Loc}>";
    }

}
