package pobserve.commons.exceptions;

public class PObserveTimeOutOfBoundException extends RuntimeException {
    public PObserveTimeOutOfBoundException() {
        super("Encountered an event with out of bound timestamp");
    }

    public PObserveTimeOutOfBoundException(String message) {
        super("Encountered an event with out of bound timestamp. " + message);
    }

    public PObserveTimeOutOfBoundException(String message, Throwable cause) {
        super("Encountered an event with out of bound timestamp." + message, cause);
    }
    public PObserveTimeOutOfBoundException(Throwable cause) {
        super(cause);
    }
}

