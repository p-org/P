package pobserve.commons.exceptions;

public class PObserveEventOutOfOrderException extends RuntimeException {
    public PObserveEventOutOfOrderException() {
        super("Encountered an out of order event.");
    }

    public PObserveEventOutOfOrderException(String message) {
        super("Encountered an out of order event.\n" + message);
    }

    public PObserveEventOutOfOrderException(String message, Throwable cause) {
        super("Encountered an out of order event.\n" + message, cause);
    }
    public PObserveEventOutOfOrderException(Throwable cause) {
        super(cause);
    }
}

