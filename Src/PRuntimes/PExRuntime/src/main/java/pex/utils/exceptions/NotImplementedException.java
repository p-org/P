package pex.utils.exceptions;

/**
 * Thrown when a method is not implemented yet.
 */
public class NotImplementedException extends PExRuntimeException {

    /**
     * Constructor
     */
    public NotImplementedException() {
        super("Not implemented yet");
    }

    /**
     * Constructor
     */
    public NotImplementedException(String message) {
        super(message);
    }
}
