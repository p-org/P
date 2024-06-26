package pexplicit.utils.monitor;

import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

import javax.annotation.concurrent.ThreadSafe;

import lombok.Getter;
import lombok.Setter;

public class TimedCall implements Callable<Integer> {
    private final Scheduler scheduler;

    @Getter
    @Setter
    private long threadId;

    public TimedCall(Scheduler scheduler, boolean resume, int localtID) {
        this.scheduler = scheduler;
        this.threadId = Thread.currentThread().getId();
        PExplicitGlobal.addTotIDtolocaltID(this.threadId, localtID);
    }

    @Override
    public Integer call()
            throws MemoutException, BugFoundException, TimeoutException, InterruptedException {
        try {
            this.scheduler.runParallel();
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
