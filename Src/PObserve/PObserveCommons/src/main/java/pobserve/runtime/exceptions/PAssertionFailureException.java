package pobserve.runtime.exceptions;

/**
 * Analogous to `Microsoft.Coyote.AssertionFailureException`: if a `Plang.Compiler...AnnounceStmt`
 * evaluates at runtime to false, throw this.
 *
 * TODO: do we need to also enclose an inner exception, as Coyote does?
 */
public class PAssertionFailureException extends RuntimeException {
    private final String assertMsg;

    private final String errorType;

    public PAssertionFailureException(String msg) {
        this.assertMsg = msg;
        this.errorType = null;
    }

    public PAssertionFailureException(String msg, String errorType) {
        this.assertMsg = msg;
        this.errorType = errorType;
    }

    @Override
    public String getMessage() {
        return "Assertion failure: " + assertMsg;
    }

    public String getErrorType() {
        return errorType;
    }
}
