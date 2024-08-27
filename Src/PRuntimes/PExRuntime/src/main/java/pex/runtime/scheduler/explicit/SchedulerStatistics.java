package pex.runtime.scheduler.explicit;

public class SchedulerStatistics {
    /**
     * Number of schedules
     */
    public int numSchedules = 0;

    /**
     * Min steps
     */
    public int minSteps = 0;

    /**
     * Max steps
     */
    public int maxSteps = 0;

    /**
     * Total steps
     */
    public int totalSteps = 0;

    /**
     * Total number of states visited
     */
    public int totalStates = 0;

    public SchedulerStatistics() {
    }
}
