package pexplicit.utils.monitor;

import lombok.Getter;
import lombok.Setter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

import pexplicit.runtime.logger.PExplicitLogger;

public class TimedCall implements Callable<Integer> {
    private final Scheduler scheduler;

    @Getter
    @Setter
    private int threadId;

    public TimedCall(Scheduler scheduler, boolean resume, int localtID) {
        this.scheduler = scheduler;
        this.threadId = localtID;
    }

    @Override
    public Integer call()
            throws MemoutException, BugFoundException, TimeoutException, InterruptedException {
        try {
            this.scheduler.runParallel(threadId);
        } catch (OutOfMemoryError e) {
            throw new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent(), e);
        } catch (NullPointerException | StackOverflowError | ClassCastException e) {
            throw new BugFoundException(e.getMessage(), e);
        } catch (MemoutException | BugFoundException | TimeoutException | InterruptedException e) {
            throw e;
        }
        return 0;
    }
}
