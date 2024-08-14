package pex.utils.exceptions;

import pex.runtime.logger.PExLogger;

public class BugFoundException extends RuntimeException {
    public BugFoundException(String message) {
        super(message);
        PExLogger.logBugFound(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
        PExLogger.logBugFound(message);
    }
}
