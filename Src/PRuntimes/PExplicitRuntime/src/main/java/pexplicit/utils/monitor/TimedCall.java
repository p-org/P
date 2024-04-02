package pexplicit.utils.monitor;

import java.util.concurrent.Callable;
import java.util.concurrent.TimeoutException;

import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.exceptions.MemoutException;

public class TimedCall implements Callable<Integer> {
  private final Scheduler scheduler;

  public TimedCall(Scheduler scheduler, boolean resume) {
    this.scheduler = scheduler;
  }

  @Override
  public Integer call()
      throws MemoutException, BugFoundException, TimeoutException, InterruptedException {
    try {
      this.scheduler.run();
    } catch (OutOfMemoryError e) {
      throw new MemoutException(e.getMessage(), MemoryMonitor.getMemSpent(), e);
    } catch (MemoutException | BugFoundException | TimeoutException | InterruptedException e) {
      throw e;
    }
      return 0;
  }
}
