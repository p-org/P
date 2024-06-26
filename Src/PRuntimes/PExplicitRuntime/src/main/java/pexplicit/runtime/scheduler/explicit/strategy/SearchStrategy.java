package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.scheduler.explicit.SearchStatistics;

import java.io.Serializable;
import java.util.*;

@Getter
public abstract class SearchStrategy implements Serializable {
    /**
     * List of all search tasks
     */
    final static List<SearchTask> allTasks = Collections.synchronizedList(new ArrayList<>());
    /**
     * Set of all search tasks that are pending
     */
    @Getter
    final static Set<Integer> pendingTasks = Collections.synchronizedSet(new HashSet<>()); // Is synchornized hash set
    /**
     * List of all search tasks that finished
     */
    @Getter
    final static List<Integer> finishedTasks = Collections.synchronizedList(new ArrayList<>());
    /**
     * Task id of the latest search task
     */
    int currTaskId = 0;
    /**
     * Starting iteration number for the current task
     */
    int currTaskStartIteration = 0;
    /**
     * Number of schedulers explored for the current task
     */
    int numSchedulesExplored = 0;

    public static SearchTask createTask(SearchTask parentTask) {
        SearchTask newTask = new SearchTask(allTasks.size(), parentTask);
        allTasks.add(newTask);
        pendingTasks.add(newTask.getId());
        return newTask;
    }

    public void createFirstTask() throws InterruptedException {
        assert (allTasks.size() == 0);
        SearchTask task = createTask(null);
        addNewTask(task);
    }

    public SearchTask getCurrTask() {
        return getTask(currTaskId);
    }

    private void setCurrTask(SearchTask task) {
        assert (pendingTasks.contains(task.getId()));
        pendingTasks.remove(task.getId());
        currTaskId = task.getId();
        currTaskStartIteration = SearchStatistics.iteration;
        numSchedulesExplored = 0;
    }

    public int getNumSchedulesInCurrTask() {
        return numSchedulesExplored;
    }

    public void incrementIteration() {
        numSchedulesExplored++;
        SearchStatistics.iteration++;
    }


    private boolean isValidTaskId(int id) {
        return (id < allTasks.size());
    }

    protected SearchTask getTask(int id) {
        assert (isValidTaskId(id));
        return allTasks.get(id);
    }

    public SearchTask setNextTask() throws InterruptedException {
        SearchTask task = popNextTask();
        if (task == null) {
            PExplicitGlobal.incrementThreadsBlocking();
            if (PExplicitGlobal.getThreadsBlocking() == PExplicitGlobal.getMaxThreads()) {
                // all threads blocked, no task remains
                return null;
            }

            // other threads still working and can add new tasks
            // try popping in every 5 seconds
            do {
                // sleep for 5 seconds
                Thread.sleep(5000);
                // try popping again
                task = popNextTask();
            } while (task == null);

            // got a non-null task
            PExplicitGlobal.decrementThreadsBlocking();
        }

        setCurrTask(task);
        return task;
    }

    /**
     * Get the number of unexplored choices in the pending tasks
     *
     * @return Number of unexplored choices
     */
    public int getNumPendingChoices() {
        int numUnexplored = 0;
        SearchTask task = getCurrTask();
        numUnexplored += task.getNumUnexploredChoices();
        for (Integer tid : pendingTasks) {
            task = getTask(tid);
            numUnexplored += task.getNumUnexploredChoices();
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
        SearchTask task = getCurrTask();
        numUnexplored += task.getNumUnexploredDataChoices();
        for (Integer tid : pendingTasks) {
            task = getTask(tid);
            numUnexplored += task.getNumUnexploredDataChoices();
        }
        return numUnexplored;
    }

    public abstract void addNewTask(SearchTask task) throws InterruptedException;

    abstract SearchTask popNextTask() throws InterruptedException;
}
