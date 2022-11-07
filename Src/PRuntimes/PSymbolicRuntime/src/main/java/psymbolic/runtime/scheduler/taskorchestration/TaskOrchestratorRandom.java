package psymbolic.runtime.scheduler.taskorchestration;

import psymbolic.runtime.scheduler.BacktrackTask;
import psymbolic.utils.RandomNumberGenerator;

import java.util.ArrayList;
import java.util.List;

public class TaskOrchestratorRandom implements TaskOrchestrator {
    private List<BacktrackTask> elements = null;

    public TaskOrchestratorRandom() {
        elements = new ArrayList<>();
    }

    public void addPriority(BacktrackTask task) {
        if (elements.contains(task)) {
            // do nothing
        } else {
            elements.add(task);
        }
    }

    public BacktrackTask getNext() {
        assert(!elements.isEmpty());
        return elements.get(RandomNumberGenerator.getInstance().getRandomInt(elements.size()));
    }

    public void remove(BacktrackTask task) throws InterruptedException {
        elements.remove(task);
    }
}
