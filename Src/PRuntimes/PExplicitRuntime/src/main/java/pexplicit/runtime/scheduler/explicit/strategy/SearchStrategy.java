package pexplicit.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pexplicit.runtime.scheduler.explicit.SearchStatistics;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.Collections;

import pexplicit.runtime.PExplicitGlobal;

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
     final static  Set<Integer> pendingTasks = Collections.synchronizedSet(new HashSet<>()); // Is synchornized hash set
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

    public SearchTask createTask(SearchTask parentTask) {
        SearchTask newTask = new SearchTask(allTasks.size(), parentTask);
        allTasks.add(newTask);
        pendingTasks.add(newTask.getId());
        return newTask;
    }

 

    
    public static void createFirstTask() {
        assert (allTasks.size() == 0);
        // SearchTask firstTask = createTask(null); // Need a static version of createTask here, so just put createTask implementation here for null argument
        SearchTask newTask = new SearchTask(allTasks.size(), null);
        allTasks.add(newTask);
        pendingTasks.add(newTask.getId());

        // setCurrTask(firstTask); // Add it to pending Task List instead of setting it to set current task; like in pendingTasks.add(newTask.getId());
    }

    public SearchTask getCurrTask() {
        return getTask(currTaskId);
    }

    private void setCurrTask(SearchTask task) {
        assert (pendingTasks.contains(task.getId()));
        pendingTasks.remove(task.getId());
        currTaskId = task.getId();
        currTaskStartIteration = SearchStatistics.iteration;
    }

    public int getNumSchedulesInCurrTask() {
        return SearchStatistics.iteration - currTaskStartIteration;
    }


    private boolean isValidTaskId(int id) {
        return (id < allTasks.size());
    }

    protected SearchTask getTask(int id) {
        assert (isValidTaskId(id));
        return allTasks.get(id);
    }

    public SearchTask setNextTask() {
        if (pendingTasks.isEmpty()) {
            PExplicitGlobal.incrementThreadsBlocking();
            while(pendingTasks.isEmpty()) {
                if (PExplicitGlobal.getThreadsBlocking() == PExplicitGlobal.getMaxThreads())
                    return null;
                Thread.sleep(50000);
            }
            PExplicitGlobal.decrementThreadsBlocking();
        }

        SearchTask nextTask = popNextTask();
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

    public abstract void addNewTask(SearchTask task);

    abstract SearchTask popNextTask();
}
