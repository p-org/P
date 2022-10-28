package psymbolic.runtime.scheduler.taskorchestration;

import psymbolic.runtime.scheduler.BacktrackTask;

import java.io.Serializable;

public interface TaskOrchestrator extends Serializable {
    void addPriority(BacktrackTask task);
    BacktrackTask getNext();
    void remove(BacktrackTask task) throws InterruptedException;
}
