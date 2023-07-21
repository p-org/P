package psym.runtime.scheduler.search.taskorchestration;

import java.util.Comparator;
import java.util.concurrent.PriorityBlockingQueue;

public class TaskOrchestratorCoverageAStar implements TaskOrchestrator {
  private final PriorityBlockingQueue<BacktrackTask> elements;

  public TaskOrchestratorCoverageAStar() {
    elements =
        new PriorityBlockingQueue<BacktrackTask>(
            100,
            new Comparator<BacktrackTask>() {
              public int compare(BacktrackTask a, BacktrackTask b) {
                return b.getPrefixCoverage().compareTo(a.getPrefixCoverage());
              }
            });
  }

  public void addPriority(BacktrackTask task) {
    elements.remove(task);
    elements.add(task);
  }

  public BacktrackTask getNext() {
    assert (!elements.isEmpty());
    return elements.peek();
  }

  public void remove(BacktrackTask task) {
    elements.remove(task);
  }
}
