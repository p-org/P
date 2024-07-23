package pexplicit.utils.exceptions;

import org.jetbrains.annotations.Async;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.scheduler.Schedule;
import pexplicit.runtime.PExplicitGlobal;

public class BugFoundException extends RuntimeException {

    @Getter
    @Setter
    private int buggyLocalTID = -1;
    
    
    @Getter
    private Schedule buggySchedule = null;

    public BugFoundException(String message) {
        super(message);
        buggyLocalTID = (PExplicitGlobal.getTID_to_localtID()).get(Thread.currentThread().getId());
        buggySchedule = (PExplicitGlobal.getSchedulers().get( (PExplicitGlobal.getTID_to_localtID()).get(Thread.currentThread().getId()) )).schedule;
        PExplicitLogger.logBugFound(message);
    }

    public BugFoundException(String message, Throwable cause) {
        super(message, cause);
        buggyLocalTID = (PExplicitGlobal.getTID_to_localtID()).get(Thread.currentThread().getId());
        buggySchedule = (PExplicitGlobal.getSchedulers().get( (PExplicitGlobal.getTID_to_localtID()).get(Thread.currentThread().getId()) )).schedule;
        PExplicitLogger.logBugFound(message);
    }
}
