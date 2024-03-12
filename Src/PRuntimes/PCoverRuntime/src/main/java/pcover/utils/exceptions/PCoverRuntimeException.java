package pcover.utils.exceptions;

/**
 * Thrown when a PCover runtime error occurs.
 */
public class PCoverRuntimeException extends RuntimeException {
    /**
     * Constructs a new PCoverRuntimeException with the specified message.
     * @param message Message to print when the error occurs
     */
    public PCoverRuntimeException(String message) {
        super("[PCover]: " + message);
    }
}
