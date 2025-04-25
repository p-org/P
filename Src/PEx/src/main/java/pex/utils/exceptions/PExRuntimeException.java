package pex.utils.exceptions;

/**
 * Thrown when a PEx runtime error occurs.
 */
public class PExRuntimeException extends RuntimeException {
    /**
     * Constructs a new PExRuntimeException with the specified message.
     *
     * @param message Message to print when the error occurs
     */
    public PExRuntimeException(String message) {
        super("[PEx]: " + message);
    }
}
