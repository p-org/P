package pex.runtime.scheduler.explicit.strategy;

import lombok.Getter;
import pex.runtime.PExGlobal;

import java.io.Serializable;
import java.util.concurrent.Semaphore;

@Getter
public abstract class SearchStrategy implements Serializable {
    private static final Semaphore sem = new Semaphore(1);

    /**
     * Task id of the latest search task
     */
    int currTaskId = 0;
    /**
     * Number of schedules for the current task
     */
    int currTaskNumSchedules = 0;

    public SearchTask getCurrTask() {
        return getTask(currTaskId);
    }

    public void setCurrTask(SearchTask task) {
        currTaskId = task.getId();
        currTaskNumSchedules = 0;
    }

    public void incrementCurrTaskNumSchedules() {
        currTaskNumSchedules++;
    }

    public SearchTask setNextTask() throws InterruptedException {
        SearchTask nextTask = popNextTask();
        if (nextTask != null) {
            if (nextTask.getId() != 0) {
                // not the very first task, read from file
                nextTask.readFromFile();
            }
        }
        return nextTask;
    }

    public SearchTask createTask(SearchTask parentTask) throws InterruptedException {
        sem.acquire();
        SearchTask newTask = new SearchTask(PExGlobal.getAllTasks().size(), parentTask);
        PExGlobal.getAllTasks().put(newTask.getId(), newTask);
        sem.release();
        return newTask;
    }

    public void createFirstTask() throws InterruptedException {
        assert (PExGlobal.getAllTasks().isEmpty());
        SearchTask firstTask = createTask(null);
        addNewTask(firstTask);
    }

    private boolean isValidTaskId(int id) {
        return (id < PExGlobal.getAllTasks().size()) && (PExGlobal.getAllTasks().containsKey(id));
    }

    protected SearchTask getTask(int id) {
        assert (isValidTaskId(id));
        return PExGlobal.getAllTasks().get(id);
    }

    public void addNewTask(SearchTask task) throws InterruptedException {
        sem.acquire();
        addTask(task);
        sem.release();
    }

    SearchTask popNextTask() throws InterruptedException {
        sem.acquire();
        SearchTask task;
        if (PExGlobal.getPendingTasks().isEmpty()) {
            task = null;
        } else {
            task = popTask();
        }
        sem.release();
        return task;
    }

    abstract SearchTask popTask();

    abstract void addTask(SearchTask task);
}
