package psym.utils.exception;

public class MemoutException extends RuntimeException {
    public final double memSpent;

    public MemoutException(String message, double memSpent) {
        super(message);
        this.memSpent = memSpent;
    }

    public MemoutException(String message, double memSpent, Throwable cause) {
        super(message,  cause);
        this.memSpent = memSpent;
    }
}
