package pobserve.commons.exceptions;

public class PObserveInternalException extends RuntimeException {
    public PObserveInternalException() {
        super("An unexpected internal error occurred. Please report this exception to PObserve Team");
    }

    public PObserveInternalException(String message) {
        super("An unexpected internal error occurred.\n"
               + " Please report this exception to PObserve Team:\n" + message);
    }

    public PObserveInternalException(String message, Throwable cause) {
        super("An unexpected internal error occurred.\n"
                + " Please report this exception to PObserve Team:\n" + message, cause);
    }
    public PObserveInternalException(Throwable cause) {
        super(cause);
    }
}

