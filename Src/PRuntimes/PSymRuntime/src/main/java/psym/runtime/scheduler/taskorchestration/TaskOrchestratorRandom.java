package psym.runtime.scheduler.taskorchestration;

import psym.runtime.scheduler.BacktrackTask;
import psym.utils.RandomNumberGenerator;

import java.util.*;

public class TaskOrchestratorRandom implements TaskOrchestrator {
    private List<BacktrackTask> elementList = null;
    private Set<BacktrackTask> elementSet = null;

    public TaskOrchestratorRandom() {
        elementList = new ArrayList<>();
        elementSet = new HashSet<>();
    }

    public void addPriority(BacktrackTask task) {
        if (elementSet.contains(task)) {
            // do nothing
        } else {
            elementList.add(task);
            elementSet.add(task);
        }
    }

    public BacktrackTask getNext() {
        assert(!elementList.isEmpty());
        Collections.shuffle(elementList, new Random(RandomNumberGenerator.getInstance().getRandomLong()));
        return elementList.get(RandomNumberGenerator.getInstance().getRandomInt(elementList.size()));
    }

    public void remove(BacktrackTask task) throws InterruptedException {
        elementList.remove(task);
        elementSet.remove(task);
    }
}
