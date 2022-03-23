package psymbolic.commandline;

public class MemoutException extends RuntimeException {
    public final double memSpent;

    public MemoutException(String message, double memSpent) {
        super(message);
        this.memSpent = memSpent;
    }
}
