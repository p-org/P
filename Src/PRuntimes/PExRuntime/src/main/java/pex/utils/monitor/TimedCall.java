package pex.utils.monitor;

import pex.runtime.scheduler.Scheduler;
import pex.utils.exceptions.BugFoundException;
import pex.utils.exceptions.MemoutException;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

public class TimedCall implements Callable<Integer> {
    private final Scheduler scheduler;

    public TimedCall(Scheduler scheduler) {
        this.scheduler = scheduler;
    }

    @Override
    public Integer call()
            throws MemoutException, BugFoundException, TimeoutException, InterruptedException {
        try {
            this.scheduler.run();
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
