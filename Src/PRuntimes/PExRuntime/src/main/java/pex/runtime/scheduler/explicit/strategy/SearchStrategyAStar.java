package pex.runtime.scheduler.explicit.strategy;

import pex.runtime.PExGlobal;

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

    public SearchStrategyAStar() {
    }

    public void addTask(SearchTask task) {
        PExGlobal.getPendingTasks().add(task);
        elements.offer(task);
    }

    public SearchTask popTask() {
        assert (!elements.isEmpty());
        SearchTask task = elements.poll();
        PExGlobal.getPendingTasks().remove(task);
        return task;
    }
}
