package psymbolic.commandline;

import psymbolic.runtime.scheduler.IterativeBoundedScheduler;

import java.util.concurrent.Callable;
import java.util.concurrent.ExecutionException;

public class TimedCall implements Callable<Integer> {
    private final IterativeBoundedScheduler scheduler;
    private final Program p;

    public TimedCall(IterativeBoundedScheduler scheduler, Program p) {
        this.scheduler = scheduler;
        this.p = p;
    }

    @Override
    public Integer call() throws MemoutException, BugFoundException {
        try {
            this.scheduler.doSearch(this.p);
        } catch (MemoutException e) {
            throw e;
        } catch (BugFoundException e) {
            throw e;
        }
        return 0;
    }
}
