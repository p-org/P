package prt.exceptions;

/**
 * Represents an assertion violation in P
 */
public class PAssertionFailureException extends RuntimeException {
    private String assertMsg;

    @Override
    public String getMessage() {
        return "Assertion failure: " + assertMsg;
    }

    public PAssertionFailureException(String msg) {
        this.assertMsg = msg;
    }
}
