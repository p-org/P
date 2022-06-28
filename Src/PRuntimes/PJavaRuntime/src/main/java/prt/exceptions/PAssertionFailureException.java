package prt.exceptions;

/**
 * Analogous to `Microsoft.Coyote.AssertionFailureException`: if a `Plang.Compiler...AnnounceStmt`
 * evaluates at runtime to false, throw this.
 *
 * TODO: do we need to also enclose an inner exception, as Coyote does?
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
