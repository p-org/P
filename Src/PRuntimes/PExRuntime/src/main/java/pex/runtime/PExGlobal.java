package pex.runtime;

import lombok.Getter;
import lombok.Setter;
import org.apache.commons.lang3.StringUtils;
import pex.commandline.PExConfig;
import pex.runtime.logger.PExLogger;
import pex.runtime.logger.ScratchLogger;
import pex.runtime.logger.StatWriter;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMachineId;
import pex.runtime.scheduler.Scheduler;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.SearchStatistics;
import pex.runtime.scheduler.explicit.StateCachingMode;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelector;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorQL;
import pex.runtime.scheduler.explicit.choiceselector.ChoiceSelectorRandom;
import pex.runtime.scheduler.explicit.strategy.SearchStrategyMode;
import pex.runtime.scheduler.explicit.strategy.SearchTask;
import pex.runtime.scheduler.replay.ReplayScheduler;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;

import java.time.Instant;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.TimeUnit;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExGlobal {
    /**
     * Map from state hash to iteration when first visited
     */
    @Getter
    private static final Map<Object, Integer> stateCache = new ConcurrentHashMap<>();
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
     * Explicit search schedulers
     **/
    private static Map<Integer, ExplicitSearchScheduler> searchSchedulers = new ConcurrentHashMap<>();
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

    public static Scheduler getScheduler() {
        if (replayScheduler == null) {
            // TODO: pex parallel - use thread id to get search scheduler
            return searchSchedulers.get(1);
        } else {
            return replayScheduler;
        }
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
        if (SearchStatistics.maxSteps < maxStepBound) {
            if (numUnexplored == 0) {
                resultString += "correct for any depth";
            } else {
                resultString += String.format("partially correct with %d choices remaining", numUnexplored);
            }
        } else {
            if (numUnexplored == 0) {
                resultString += String.format("correct up to step %d", SearchStatistics.maxSteps);
            } else {
                resultString += String.format("partially correct up to step %d with %d choices remaining", SearchStatistics.maxSteps, numUnexplored);
            }
        }
        result = resultString;
    }

    public static void recordStats() {
        printProgress(true);

        // print basic statistics
        StatWriter.log("#-schedules", String.format("%d", SearchStatistics.iteration));
        StatWriter.log("#-timelines", String.format("%d", timelines.size()));
        if (config.getStateCachingMode() != StateCachingMode.None) {
            StatWriter.log("#-states", String.format("%d", SearchStatistics.totalStates));
            StatWriter.log("#-distinct-states", String.format("%d", SearchStatistics.totalDistinctStates));
        }
        StatWriter.log("steps-min", String.format("%d", SearchStatistics.minSteps));
        StatWriter.log("steps-avg", String.format("%d", SearchStatistics.totalSteps / SearchStatistics.iteration));
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
        s.append(String.format("\n      Schedules:        %d", SearchStatistics.iteration));
        s.append(String.format("\n      Unexplored:       %d", getNumUnexploredChoices()));
        s.append(String.format("\n      FinishedTasks:    %d", finishedTasks.size()));
        s.append(String.format("\n      PendingTasks:     %d", pendingTasks.size()));
        s.append(String.format("\n      Timelines:        %d", timelines.size()));
        if (config.getStateCachingMode() != StateCachingMode.None) {
            s.append(String.format("\n      States:           %d", SearchStatistics.totalStates));
            s.append(String.format("\n      DistinctStates:   %d", SearchStatistics.totalDistinctStates));
        }
        ScratchLogger.log(s.toString());
    }

    public static void printProgressHeader(boolean consolePrint) {
        StringBuilder s = new StringBuilder(100);
        s.append(StringUtils.center("Time", 11));
        s.append(StringUtils.center("Memory", 9));

        s.append(StringUtils.center("Schedules", 12));
        s.append(StringUtils.center("Timelines", 12));
        s.append(StringUtils.center("Unexplored", 24));

        if (config.getStateCachingMode() != StateCachingMode.None) {
            s.append(StringUtils.center("States", 12));
        }

        if (consolePrint) {
            System.out.println("--------------------");
            System.out.println(s);
        } else {
            PExLogger.logVerbose("--------------------");
            PExLogger.logVerbose(s.toString());
        }
    }

    public static void printProgress(boolean forcePrint) {
        if (forcePrint || (TimeMonitor.findInterval(lastReportTime) > 10)) {
            lastReportTime = Instant.now();
            double newRuntime = TimeMonitor.getRuntime();
            printCurrentStatus(newRuntime);
            boolean consolePrint = (config.getVerbosity() == 0);
            if (consolePrint || forcePrint) {
                long runtime = (long) (newRuntime * 1000);
                String runtimeHms =
                        String.format(
                                "%02d:%02d:%02d",
                                TimeUnit.MILLISECONDS.toHours(runtime),
                                TimeUnit.MILLISECONDS.toMinutes(runtime) % TimeUnit.HOURS.toMinutes(1),
                                TimeUnit.MILLISECONDS.toSeconds(runtime) % TimeUnit.MINUTES.toSeconds(1));

                StringBuilder s = new StringBuilder(100);
                if (consolePrint) {
                    s.append('\r');
                } else {
                    printProgressHeader(false);
                }
                s.append(StringUtils.center(String.format("%s", runtimeHms), 11));
                s.append(
                        StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));

                s.append(StringUtils.center(String.format("%d", SearchStatistics.iteration), 12));
                s.append(StringUtils.center(String.format("%d", timelines.size()), 12));
                s.append(
                        StringUtils.center(
                                String.format(
                                        "%d (%.0f %% data)", getNumUnexploredChoices(), getUnexploredDataChoicesPercent()),
                                24));

                if (config.getStateCachingMode() != StateCachingMode.None) {
                    s.append(StringUtils.center(String.format("%d", SearchStatistics.totalDistinctStates), 12));
                }

                if (consolePrint) {
                    System.out.print(s);
                } else {
                    PExLogger.logVerbose(s.toString());
                }
            }
        }
    }
}