package pex.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pex.runtime.PExGlobal;
import pex.runtime.scheduler.explicit.SearchStatistics;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Getter
public abstract class SearchStrategy implements Serializable {
    /**
     * Task id of the latest search task
     */
    int currTaskId = 0;
    /**
     * Starting iteration number for the current task
     */
    int currTaskStartIteration = 0;

    public SearchTask createTask(SearchTask parentTask) {
        SearchTask newTask = new SearchTask(PExGlobal.getAllTasks().size(), parentTask);
        PExGlobal.getAllTasks().put(newTask.getId(), newTask);
        return newTask;
    }

    public void createFirstTask() {
        assert (PExGlobal.getAllTasks().size() == 0);
        SearchTask firstTask = createTask(null);
        PExGlobal.getPendingTasks().add(firstTask);
        setCurrTask(firstTask);
    }

    public SearchTask getCurrTask() {
        return getTask(currTaskId);
    }

    private void setCurrTask(SearchTask task) {
        assert (PExGlobal.getPendingTasks().contains(task));
        PExGlobal.getPendingTasks().remove(task);
        currTaskId = task.getId();
        currTaskStartIteration = SearchStatistics.iteration;
    }

    public int getNumSchedulesInCurrTask() {
        return SearchStatistics.iteration - currTaskStartIteration;
    }


    private boolean isValidTaskId(int id) {
        return (id < PExGlobal.getAllTasks().size()) && (PExGlobal.getAllTasks().containsKey(id));
    }

    protected SearchTask getTask(int id) {
        assert (isValidTaskId(id));
        return PExGlobal.getAllTasks().get(id);
    }

    public SearchTask setNextTask() {
        if (PExGlobal.getPendingTasks().isEmpty()) {
            return null;
        }

        SearchTask nextTask = popNextTask();
        nextTask.readFromFile();
        setCurrTask(nextTask);

        return nextTask;
    }

    /**
     * Get the number of unexplored choices in the pending tasks
     *
     * @return Number of unexplored choices
     */
    public int getNumPendingChoices() {
        int numUnexplored = 0;
        for (SearchTask task : PExGlobal.getPendingTasks()) {
            numUnexplored += task.getTotalUnexploredChoices();
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in the pending tasks
     *
     * @return Number of unexplored data choices
     */
    public int getNumPendingDataChoices() {
        int numUnexplored = 0;
        for (SearchTask task : PExGlobal.getPendingTasks()) {
            numUnexplored += task.getTotalUnexploredDataChoices();
        }
        return numUnexplored;
    }

    abstract SearchTask popNextTask();

    public abstract void addNewTask(SearchTask task);
}
