package psymbolic.commandline;

import psymbolic.runtime.scheduler.IterativeBoundedScheduler;

import java.util.concurrent.Callable;

public class TimedCall implements Callable<Integer> {
    private final IterativeBoundedScheduler scheduler;
    private final Program p;
    private boolean resume;

    public TimedCall(IterativeBoundedScheduler scheduler, Program p, boolean resume) {
        this.scheduler = scheduler;
        this.p = p;
        this.resume = resume;
    }

    @Override
    public Integer call() throws MemoutException, BugFoundException {
        try {
            if (!this.resume)
                this.scheduler.doSearch(this.p);
            else
                this.scheduler.resumeSearch(this.p);
        } catch (MemoutException e) {
            throw e;
        } catch (BugFoundException e) {
            throw e;
        }
        return 0;
    }
}
