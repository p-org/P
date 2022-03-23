package psymbolic.commandline;

public class TimeoutException extends RuntimeException {
    public final double timeSpent;

    public TimeoutException(String message, double timeSpent) {
        super(message);
        this.timeSpent = timeSpent;
    }
}
