package psym.utils.exception;

import psym.valuesummary.Guard;

public class BugFoundException extends RuntimeException {
    public final Guard pathConstraint;

    public BugFoundException(String message, Guard pathConstraint) {
        super(message);
        this.pathConstraint = pathConstraint;
    }

    public BugFoundException(String message, Guard pathConstraint, Throwable cause) {
        super(message, cause);
        this.pathConstraint = pathConstraint;
    }
}
