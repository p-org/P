package pexplicit.utils.exceptions;

public class BugFoundException extends RuntimeException {
    public BugFoundException(String message) {
        super(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
    }
}
