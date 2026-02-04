package pobserve.commons.exceptions;

public class PObserveLogParsingException extends Exception {
    public PObserveLogParsingException() {
        super("An error occurred while parsing the log.");
    }

    public PObserveLogParsingException(String message) {
        super(message);
    }

    public PObserveLogParsingException(String message, Throwable cause) {
        super(message, cause);
    }
    public PObserveLogParsingException(Throwable cause) {
        super(cause);
    }
}
