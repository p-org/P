package pex.runtime.scheduler.explicit.strategy;

import pex.runtime.PExGlobal;

public class SearchStrategyDfs extends SearchStrategy {
    public SearchStrategyDfs() {
    }

    public void addTask(SearchTask task) {
        PExGlobal.getPendingTasks().add(task);
    }

    public SearchTask popTask() {
        assert (PExGlobal.getPendingTasks().size() == 1);
        SearchTask task = PExGlobal.getPendingTasks().iterator().next();
        PExGlobal.getPendingTasks().remove(task);
        return task;
    }
}
