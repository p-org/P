package pex.runtime.scheduler.explicit.strategy;

import pex.runtime.PExGlobal;

public class SearchStrategyDfs extends SearchStrategy {
    public SearchStrategyDfs() {
    }

    public void addNewTask(SearchTask task) {
    }

    public SearchTask popNextTask() {
        assert (PExGlobal.getPendingTasks().size() == 1);
        return PExGlobal.getPendingTasks().iterator().next();
    }
}
