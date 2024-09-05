package pex.runtime;

import lombok.Getter;
import lombok.Setter;
import org.apache.commons.lang3.StringUtils;
import pex.commandline.PExConfig;
import pex.runtime.logger.ScratchLogger;
import pex.runtime.logger.StatWriter;
import pex.runtime.scheduler.Scheduler;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.StateCachingMode;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelector;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorQL;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorRandom;
import pex.runtime.scheduler.explicit.strategy.SearchTask;
import pex.runtime.scheduler.replay.ReplayScheduler;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;

import java.time.Instant;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExGlobal {
    /**
     * Map from state hash to schedulerId-iteration when first visited
     */
    @Getter
    private static final Map<Object, String> stateCache = new ConcurrentHashMap<>();
    /**
     * Set of timelines
     */
    @Getter
    private static final Set<Object> timelines = ConcurrentHashMap.newKeySet();
    /**
     * List of all search tasks
     */
    @Getter
    private static final Map<Integer, SearchTask> allTasks = new ConcurrentHashMap<>();
    /**
     * Set of all search tasks that are pending
     */
    @Getter
    private static final Set<SearchTask> pendingTasks = ConcurrentHashMap.newKeySet();
    /**
     * List of all search tasks that finished
     */
    @Getter
    private static final Set<SearchTask> finishedTasks = ConcurrentHashMap.newKeySet();
    /**
     * Set of all search tasks that are currently running
     */
    @Getter
    private static final Set<SearchTask> runningTasks = ConcurrentHashMap.newKeySet();
    /**
     * Explicit search schedulers
     **/
    @Getter
    private static final Map<Integer, ExplicitSearchScheduler> searchSchedulers = new ConcurrentHashMap<>();
    @Getter
    private static final Map<Long, Integer> threadToSchedulerId = new ConcurrentHashMap<>();
    /**
     * Map from scheduler to global machine id
     */
    private static final Map<Scheduler, Integer> globalMachineId = new ConcurrentHashMap<>();
    /**
     * Map from scheduler to global monitor id
     */
    private static final Map<Scheduler, Integer> globalMonitorId = new ConcurrentHashMap<>();
    /**
     * PModel
     **/
    @Getter
    @Setter
    private static PModel model = null;
    /**
     * Global configuration
     **/
    @Getter
    @Setter
    private static PExConfig config = null;
    /**
     * Replay scheduler
     */
    @Setter
    private static ReplayScheduler replayScheduler = null;
    /**
     * Status of the run
     **/
    @Getter
    @Setter
    private static STATUS status = STATUS.INCOMPLETE;
    /**
     * Result of the run
     **/
    @Getter
    @Setter
    private static String result = "error";
    /**
     * Choice orchestrator
     */
    @Getter
    private static ChoiceSelector choiceSelector = null;
    /**
     * Time of last status report
     */
    private static Instant lastReportTime = Instant.now();

    public static void addSearchScheduler(ExplicitSearchScheduler sch) {
        searchSchedulers.put(sch.getSchedulerId(), sch);
    }

    public static void registerSearchScheduler(int schId) {
        assert (searchSchedulers.containsKey(schId));

        long threadId = Thread.currentThread().getId();
        assert (!threadToSchedulerId.containsKey(threadId));
        threadToSchedulerId.put(threadId, schId);
    }

    public static Scheduler getScheduler() {
        if (replayScheduler == null) {
            long threadId = Thread.currentThread().getId();
            Integer schId = threadToSchedulerId.get(threadId);
            assert (schId != null);
            return searchSchedulers.get(schId);
        } else {
            return replayScheduler;
        }
    }

    public static int getGlobalMachineId() {
        Scheduler sch = getScheduler();
        if (!globalMachineId.containsKey(sch)) {
            globalMachineId.put(sch, 1);
        }
        return globalMachineId.get(sch);
    }

    public static void setGlobalMachineId(int id) {
        Scheduler sch = getScheduler();
        globalMachineId.put(sch, id);
    }

    public static int getGlobalMonitorId() {
        Scheduler sch = getScheduler();
        if (!globalMonitorId.containsKey(sch)) {
            globalMonitorId.put(sch, -1);
        }
        return globalMonitorId.get(sch);
    }

    public static void setGlobalMonitorId(int id) {
        Scheduler sch = getScheduler();
        globalMonitorId.put(sch, id);
    }

    /**
     * Set choice orchestrator
     */
    public static void setChoiceSelector() {
        switch (config.getChoiceSelectorMode()) {
            case Random:
                choiceSelector = new ChoiceSelectorRandom();
                break;
            case QL:
                choiceSelector = new ChoiceSelectorQL();
                break;
            default:
                throw new RuntimeException("Unrecognized choice orchestrator: " + config.getChoiceSelectorMode());
        }
    }

    /**
     * Get the number of unexplored choices in the pending tasks
     *
     * @return Number of unexplored choices
     */
    static int getNumPendingChoices() {
        int numUnexplored = 0;
        for (SearchTask task : pendingTasks) {
            numUnexplored += task.getTotalUnexploredChoices();
        }
        return numUnexplored;
    }

    /**
     * Get the number of unexplored data choices in the pending tasks
     *
     * @return Number of unexplored data choices
     */
    static int getNumPendingDataChoices() {
        int numUnexplored = 0;
        for (SearchTask task : pendingTasks) {
            numUnexplored += task.getTotalUnexploredDataChoices();
        }
        return numUnexplored;
    }

    static int getNumUnexploredChoices() {
        int result = getNumPendingChoices();
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getSearchStrategy().getCurrTask().getCurrentNumUnexploredChoices();
        }
        return result;
    }

    static int getNumUnexploredDataChoices() {
        int result = getNumPendingDataChoices();
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getSearchStrategy().getCurrTask().getCurrentNumUnexploredDataChoices();
        }
        return result;
    }

    /**
     * Get the percentage of unexplored choices that are data choices
     *
     * @return Percentage of unexplored choices that are data choices
     */
    static double getUnexploredDataChoicesPercent() {
        int totalUnexplored = getNumUnexploredChoices();
        if (totalUnexplored == 0) {
            return 0;
        }

        int numUnexploredData = getNumUnexploredDataChoices();
        return (numUnexploredData * 100.0) / totalUnexplored;
    }

    public static void updateResult() {
        if (status == STATUS.BUG_FOUND) {
            return;
        }

        String resultString = "";
        int maxStepBound = config.getMaxStepBound();
        int numUnexplored = getNumUnexploredChoices();
        if (getMaxSteps() < maxStepBound) {
            if (numUnexplored == 0) {
                resultString += "correct for any depth";
            } else {
                resultString += String.format("partially correct with %d choices remaining", numUnexplored);
            }
        } else {
            if (numUnexplored == 0) {
                resultString += String.format("correct up to step %d", getMaxSteps());
            } else {
                resultString += String.format("partially correct up to step %d with %d choices remaining", getMaxSteps(), numUnexplored);
            }
        }
        result = resultString;
    }

    public static void recordStats() {
        // print basic statistics
        StatWriter.log("#-schedules", String.format("%d", getTotalSchedules()));
        StatWriter.log("#-timelines", String.format("%d", timelines.size()));
        if (config.getStateCachingMode() != StateCachingMode.None) {
            StatWriter.log("#-states", String.format("%d", getTotalStates()));
            StatWriter.log("#-distinct-states", String.format("%d", stateCache.size()));
        }
        StatWriter.log("steps-min", String.format("%d", getMinSteps()));
        StatWriter.log("steps-avg", String.format("%d", getTotalSteps() / getTotalSchedules()));
        StatWriter.log("#-choices-unexplored", String.format("%d", getNumUnexploredChoices()));
        StatWriter.log("%-choices-unexplored-data", String.format("%.1f", getUnexploredDataChoicesPercent()));
        StatWriter.log("#-tasks-finished", String.format("%d", finishedTasks.size()));
        StatWriter.log("#-tasks-pending", String.format("%d", pendingTasks.size()));
        StatWriter.log("ql-#-states", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumStates()));
        StatWriter.log("ql-#-actions", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumActions()));
    }

    static void printCurrentStatus(double newRuntime) {
        StringBuilder s = new StringBuilder("--------------------");
        s.append(String.format("\n    Status after %.2f seconds:", newRuntime));
        s.append(String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
        s.append(String.format("\n      Schedules:        %d", getTotalSchedules()));
//        s.append(String.format("\n      Unexplored:       %d", getNumUnexploredChoices()));
        s.append(String.format("\n      RunningTasks:     %d", runningTasks.size()));
        s.append(String.format("\n      FinishedTasks:    %d", finishedTasks.size()));
        s.append(String.format("\n      PendingTasks:     %d", pendingTasks.size()));
        s.append(String.format("\n      Timelines:        %d", timelines.size()));
        if (config.getStateCachingMode() != StateCachingMode.None) {
            s.append(String.format("\n      States:           %d", getTotalStates()));
            s.append(String.format("\n      DistinctStates:   %d", stateCache.size()));
        }
        ScratchLogger.log(s.toString());
    }

    public static void printProgressHeader() {
        StringBuilder s = new StringBuilder(100);
        s.append(StringUtils.center("Time", 11));
        s.append(StringUtils.center("Memory", 9));

        s.append(StringUtils.center("Schedules", 12));
        s.append(StringUtils.center("Timelines", 12));
        s.append(StringUtils.center("Tasks (run/fin/pen)", 24));
//        s.append(StringUtils.center("Unexplored", 24));

        if (config.getStateCachingMode() != StateCachingMode.None) {
            s.append(StringUtils.center("States", 12));
        }

        System.out.println("--------------------");
        System.out.println(s);
    }

    public static void printProgressFooter() {
        System.out.println();
    }

    public static void printProgress(boolean forcePrint) {
        if (forcePrint || (TimeMonitor.findInterval(lastReportTime) > 10)) {
            lastReportTime = Instant.now();
            double newRuntime = TimeMonitor.getRuntime();
            printCurrentStatus(newRuntime);
            long runtime = (long) (newRuntime * 1000);
            String runtimeHms =
                    String.format(
                            "%02d:%02d:%02d",
                            TimeUnit.MILLISECONDS.toHours(runtime),
                            TimeUnit.MILLISECONDS.toMinutes(runtime) % TimeUnit.HOURS.toMinutes(1),
                            TimeUnit.MILLISECONDS.toSeconds(runtime) % TimeUnit.MINUTES.toSeconds(1));

            StringBuilder s = new StringBuilder(100);
            s.append('\r');
            s.append(StringUtils.center(String.format("%s", runtimeHms), 11));
            s.append(
                    StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));

            s.append(StringUtils.center(String.format("%d", getTotalSchedules()), 12));
            s.append(StringUtils.center(String.format("%d", timelines.size()), 12));
            s.append(StringUtils.center(String.format("%d / %d / %d",
                            runningTasks.size(), finishedTasks.size(), pendingTasks.size()),
                    24));
//                s.append(
//                        StringUtils.center(
//                                String.format(
//                                        "%d (%.0f %% data)", getNumUnexploredChoices(), getUnexploredDataChoicesPercent()),
//                                24));

            if (config.getStateCachingMode() != StateCachingMode.None) {
                s.append(StringUtils.center(String.format("%d", stateCache.size()), 12));
            }

            System.out.print(s);
        }
    }

    public static int getTotalSchedules() {
        int result = 0;
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getStats().numSchedules;
        }
        return result;
    }

    public static int getMinSteps() {
        int result = -1;
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            if (result == -1 || result > sch.getStats().minSteps) {
                result = sch.getStats().minSteps;
            }
        }
        return result;
    }

    public static int getMaxSteps() {
        int result = -1;
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            if (result == -1 || result < sch.getStats().maxSteps) {
                result = sch.getStats().maxSteps;
            }
        }
        return result;
    }

    public static int getTotalSteps() {
        int result = 0;
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getStats().totalSteps;
        }
        return result;
    }

    public static int getTotalStates() {
        int result = 0;
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getStats().totalStates;
        }
        return result;
    }
}