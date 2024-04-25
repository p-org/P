package pexplicit.runtime.scheduler.explicit.strategy;

import java.util.Comparator;
import java.util.concurrent.PriorityBlockingQueue;

public class SearchStrategyAStar extends SearchStrategy {
  private final PriorityBlockingQueue<SearchTask> elements;

  public SearchStrategyAStar() {
    elements =
        new PriorityBlockingQueue<SearchTask>(
            100,
            new Comparator<SearchTask>() {
              public int compare(SearchTask a, SearchTask b) {
                return Integer.compare(a.getCurrChoiceNumber(), b.getCurrChoiceNumber());
              }
            });
  }

  public void addNewTask(SearchTask task) {
    elements.offer(task);
  }

  public SearchTask popNextTask() {
    assert (!elements.isEmpty());
    return elements.poll();
  }
}
