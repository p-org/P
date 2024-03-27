package pexplicit.utils.exceptions;

/**
 * Thrown when a PExplicit runtime error occurs.
 */
public class PExplicitRuntimeException extends RuntimeException {
    /**
     * Constructs a new PExplicitRuntimeException with the specified message.
     *
     * @param message Message to print when the error occurs
     */
    public PExplicitRuntimeException(String message) {
        super("[PExplicit]: " + message);
    }
}
