package pex.utils.exceptions;

import lombok.Getter;
import pex.runtime.PExGlobal;
import pex.runtime.scheduler.Scheduler;

public class BugFoundException extends RuntimeException {
    @Getter
    Scheduler scheduler;

    public BugFoundException(String message) {
        super(message);
        this.scheduler = PExGlobal.getScheduler();
        this.scheduler.getLogger().logBugFound(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
        this.scheduler = PExGlobal.getScheduler();
        this.scheduler.getLogger().logBugFound(message);
    }
}
