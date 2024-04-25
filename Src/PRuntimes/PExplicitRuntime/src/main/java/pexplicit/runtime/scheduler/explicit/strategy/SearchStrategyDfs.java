package pexplicit.runtime.scheduler.explicit.strategy;

public class SearchStrategyDfs extends SearchStrategy {
    public SearchStrategyDfs() {
    }

    public void addNewTask(SearchTask task) {
    }

    public SearchTask popNextTask() {
        throw new RuntimeException("Cannot pop the next task in dfs strategy since there should be just a single task");
    }
}
