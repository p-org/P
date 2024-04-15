package pexplicit.utils.exceptions;

import pexplicit.runtime.logger.PExplicitLogger;

public class BugFoundException extends RuntimeException {
    public BugFoundException(String message) {
        super(message);
        PExplicitLogger.logBugFound(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
        PExplicitLogger.logBugFound(message);
    }
}
