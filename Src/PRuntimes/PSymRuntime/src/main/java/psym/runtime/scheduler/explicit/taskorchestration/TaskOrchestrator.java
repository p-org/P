package psym.runtime.scheduler.explicit.taskorchestration;

import java.io.Serializable;
import psym.runtime.scheduler.explicit.BacktrackTask;

public interface TaskOrchestrator extends Serializable {
    void addPriority(BacktrackTask task);

    BacktrackTask getNext();

    void remove(BacktrackTask task) throws InterruptedException;
}
