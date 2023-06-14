package psym.runtime.scheduler.taskorchestration;

import psym.runtime.scheduler.BacktrackTask;

import java.util.Comparator;
import java.util.concurrent.PriorityBlockingQueue;

public class TaskOrchestratorCoverageAStar implements TaskOrchestrator {
    private PriorityBlockingQueue<BacktrackTask> elements = null;

    public TaskOrchestratorCoverageAStar() {
        elements = new PriorityBlockingQueue<BacktrackTask>(100, new Comparator<BacktrackTask>() {
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

    public void remove(BacktrackTask task) throws InterruptedException {
        elements.remove(task);
    }

}
