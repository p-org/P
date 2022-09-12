package psymbolic.commandline;

import psymbolic.runtime.scheduler.IterativeBoundedScheduler;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

public class TimedCall implements Callable<Integer> {
    private final IterativeBoundedScheduler scheduler;
    private boolean resume;

    public TimedCall(IterativeBoundedScheduler scheduler, boolean resume) {
        this.scheduler = scheduler;
        this.resume = resume;
    }

    @Override
    public Integer call() throws MemoutException, BugFoundException, TimeoutException {
        try {
            if (!this.resume)
                this.scheduler.doSearch();
            else
                this.scheduler.resumeSearch();
        } catch (MemoutException e) {
            throw e;
        } catch (BugFoundException e) {
            throw e;
        } catch (TimeoutException e) {
            throw e;
        }
        return 0;
    }
}
