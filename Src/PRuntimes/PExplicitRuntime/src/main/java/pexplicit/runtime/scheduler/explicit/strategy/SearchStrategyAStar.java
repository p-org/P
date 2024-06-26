package pexplicit.runtime.scheduler.explicit.strategy;

import java.util.Comparator;
import java.util.concurrent.PriorityBlockingQueue;

public class SearchStrategyAStar extends SearchStrategy {
    private static final PriorityBlockingQueue<SearchTask> elements =
            new PriorityBlockingQueue<SearchTask>(
                    100,
                    new Comparator<SearchTask>() {
                        public int compare(SearchTask a, SearchTask b) {
                            return Integer.compare(a.getCurrChoiceNumber(), b.getCurrChoiceNumber());
                        }
                    });

    public SearchStrategyAStar() {}

    public void addNewTask(SearchTask task) {
        elements.offer(task);
    }

    public SearchTask popNextTask() {
        return elements.poll();
    }
}
