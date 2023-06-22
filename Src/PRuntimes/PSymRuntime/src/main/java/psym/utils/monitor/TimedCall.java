package psym.utils.monitor;

import psym.runtime.scheduler.SearchScheduler;
import psym.utils.exception.BugFoundException;
import psym.utils.exception.MemoutException;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

public class TimedCall implements Callable<Integer> {
    private final SearchScheduler scheduler;
    private final boolean resume;

    public TimedCall(SearchScheduler scheduler, boolean resume) {
        this.scheduler = scheduler;
        this.resume = resume;
    }

    @Override
    public Integer call() throws MemoutException, BugFoundException, TimeoutException, InterruptedException {
        try {
            if (!this.resume)
                this.scheduler.doSearch();
            else
                this.scheduler.resumeSearch();
        } catch (OutOfMemoryError e) {
            throw new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent());
        } catch (MemoutException e) {
            throw e;
        } catch (BugFoundException e) {
            throw e;
        } catch (TimeoutException e) {
            throw e;
        } catch (InterruptedException e) {
            throw e;
        }
        return 0;
    }
}
