package psymbolic.runtime.scheduler.orchestration;

import psymbolic.runtime.scheduler.BacktrackTask;

import java.io.Serializable;

public interface Orchestrator extends Serializable {
    void addPriority(BacktrackTask task);
    BacktrackTask getNext();
    void remove(BacktrackTask task) throws InterruptedException;
}
