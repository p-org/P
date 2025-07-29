package pex.runtime.scheduler.explicit;

public class SchedulerStatistics {
    /**
     * Number of schedules
     */
    public int numSchedules = 0;

    /**
     * Min steps
     */
    public int minSteps = -1;

    /**
     * Max steps
     */
    public int maxSteps = -1;

    /**
     * Total steps
     */
    public long totalSteps = 0;

    /**
     * Total number of states visited
     */
    public int totalStates = 0;

    public SchedulerStatistics() {
    }
}
