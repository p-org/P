package psym.utils.exception;

import psym.runtime.PSymGlobal;
import psym.runtime.logger.TextWriter;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.valuesummary.Guard;

public class BugFoundException extends RuntimeException {
    public final Guard pathConstraint;

    public BugFoundException(String message, Guard pathConstraint) {
        super(message);
        this.pathConstraint = pathConstraint;
        if (PSymGlobal.getScheduler() instanceof ReplayScheduler) {
            TextWriter.logBug(message);
        }
    }

    public BugFoundException(String message, Guard pathConstraint, Throwable cause) {
        super(message, cause);
        this.pathConstraint = pathConstraint;
    }
}
