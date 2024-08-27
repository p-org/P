package pex.utils.exceptions;

import pex.runtime.PExGlobal;

public class BugFoundException extends RuntimeException {
    public BugFoundException(String message) {
        super(message);
        PExGlobal.getScheduler().getLogger().logBugFound(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
        PExGlobal.getScheduler().getLogger().logBugFound(message);
    }
}
