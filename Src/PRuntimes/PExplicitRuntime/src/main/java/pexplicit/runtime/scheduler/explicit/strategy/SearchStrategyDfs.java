package pexplicit.runtime.scheduler.explicit.strategy;

public class SearchStrategyDfs extends SearchStrategy {
    public SearchStrategyDfs() {
    }

    public void addNewTask(SearchTask task) {
    }

    public SearchTask popNextTask() {
        assert (pendingTasks.size() == 1);
        return getTask(pendingTasks.iterator().next());
    }
}
