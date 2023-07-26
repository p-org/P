package psym.runtime.scheduler.search.taskorchestration;

import java.io.Serializable;

public interface TaskOrchestrator extends Serializable {
    void addPriority(BacktrackTask task);

    BacktrackTask getNext();

    void remove(BacktrackTask task) throws InterruptedException;
}
