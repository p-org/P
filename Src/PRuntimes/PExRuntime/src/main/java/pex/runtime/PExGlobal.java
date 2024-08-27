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
     * Mapping from machine type to list of all machine instances
     */
    @Getter
    private static final Map<Class<? extends PMachine>, List<PMachine>> machineListByType = new HashMap<>();
    /**
     * Set of machines
     */
    @Getter
    private static final SortedSet<PMachine> machineSet = new TreeSet<>();
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
    @Getter
    @Setter
    private static SearchStrategyMode searchStrategyMode;
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
    @Getter
    @Setter
    private static Instant lastReportTime = Instant.now();

    public static void addSearchScheduler(ExplicitSearchScheduler sch) {
        searchSchedulers.put(sch.getSchedulerId(), sch);
    }

    public static Scheduler getScheduler() {
        if (replayScheduler == null) {
            // TODO
            return searchSchedulers.get(1);
        } else {
            return replayScheduler;
        }
    }

    /**
     * Get a machine of a given type and index if exists, else return null.
     *
     * @param pid Machine pid
     * @return Machine
     */
    public static PMachine getGlobalMachine(PMachineId pid) {
        List<PMachine> machinesOfType = machineListByType.get(pid.getType());
        if (machinesOfType == null) {
            return null;
        }
        if (pid.getTypeId() >= machinesOfType.size()) {
            return null;
        }
        PMachine result = machineListByType.get(pid.getType()).get(pid.getTypeId());
        assert (machineSet.contains(result));
        return result;
    }

    /**
     * Add a machine.
     *
     * @param machine      Machine to add
     * @param machineCount Machine type count
     */
    public static void addGlobalMachine(PMachine machine, int machineCount) {
        if (!machineListByType.containsKey(machine.getClass())) {
            machineListByType.put(machine.getClass(), new ArrayList<>());
        }
        assert (machineCount == machineListByType.get(machine.getClass()).size());
        machineListByType.get(machine.getClass()).add(machine);
        machineSet.add(machine);
        assert (machineListByType.get(machine.getClass()).get(machineCount) == machine);
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
    public static int getNumPendingChoices() {
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
    public static int getNumPendingDataChoices() {
        int numUnexplored = 0;
        for (SearchTask task : pendingTasks) {
            numUnexplored += task.getTotalUnexploredDataChoices();
        }
        return numUnexplored;
    }

    public static int getNumUnexploredChoices() {
        int result = getNumPendingChoices();
        for (ExplicitSearchScheduler sch : searchSchedulers.values()) {
            result += sch.getSearchStrategy().getCurrTask().getCurrentNumUnexploredChoices();
        }
        return result;
    }

    public static int getNumUnexploredDataChoices() {
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
    public static double getUnexploredDataChoicesPercent() {
        int totalUnexplored = getNumUnexploredChoices();
        if (totalUnexplored == 0) {
            return 0;
        }

        int numUnexploredData = getNumUnexploredDataChoices();
        return (numUnexploredData * 100.0) / totalUnexplored;
    }

    public static void updateResult() {
        if (PExGlobal.getStatus() == STATUS.BUG_FOUND) {
            return;
        }

        String result = "";
        int maxStepBound = PExGlobal.getConfig().getMaxStepBound();
        int numUnexplored = getNumUnexploredChoices();
        if (SearchStatistics.maxSteps < maxStepBound) {
            if (numUnexplored == 0) {
                result += "correct for any depth";
            } else {
                result += String.format("partially correct with %d choices remaining", numUnexplored);
            }
        } else {
            if (numUnexplored == 0) {
                result += String.format("correct up to step %d", SearchStatistics.maxSteps);
            } else {
                result += String.format("partially correct up to step %d with %d choices remaining", SearchStatistics.maxSteps, numUnexplored);
            }
        }
        PExGlobal.setResult(result);
    }

    public static void recordStats() {
        printProgress(true);

        // print basic statistics
        StatWriter.log("#-schedules", String.format("%d", SearchStatistics.iteration));
        StatWriter.log("#-timelines", String.format("%d", PExGlobal.getTimelines().size()));
        if (PExGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
            StatWriter.log("#-states", String.format("%d", SearchStatistics.totalStates));
            StatWriter.log("#-distinct-states", String.format("%d", SearchStatistics.totalDistinctStates));
        }
        StatWriter.log("steps-min", String.format("%d", SearchStatistics.minSteps));
        StatWriter.log("steps-avg", String.format("%d", SearchStatistics.totalSteps / SearchStatistics.iteration));
        StatWriter.log("#-choices-unexplored", String.format("%d", getNumUnexploredChoices()));
        StatWriter.log("%-choices-unexplored-data", String.format("%.1f", getUnexploredDataChoicesPercent()));
        StatWriter.log("#-tasks-finished", String.format("%d", PExGlobal.getFinishedTasks().size()));
        StatWriter.log("#-tasks-pending", String.format("%d", PExGlobal.getFinishedTasks().size()));
        StatWriter.log("ql-#-states", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumStates()));
        StatWriter.log("ql-#-actions", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumActions()));
    }

    public static void printCurrentStatus(double newRuntime) {
        StringBuilder s = new StringBuilder("--------------------");
        s.append(String.format("\n    Status after %.2f seconds:", newRuntime));
        s.append(String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
        s.append(String.format("\n      Schedules:        %d", SearchStatistics.iteration));
        s.append(String.format("\n      Unexplored:       %d", getNumUnexploredChoices()));
        s.append(String.format("\n      FinishedTasks:    %d", PExGlobal.getFinishedTasks().size()));
        s.append(String.format("\n      PendingTasks:     %d", PExGlobal.getPendingTasks().size()));
        s.append(String.format("\n      Timelines:        %d", PExGlobal.getTimelines().size()));
        if (PExGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
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

        if (PExGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
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
        if (forcePrint || (TimeMonitor.findInterval(getLastReportTime()) > 10)) {
            setLastReportTime(Instant.now());
            double newRuntime = TimeMonitor.getRuntime();
            printCurrentStatus(newRuntime);
            boolean consolePrint = (PExGlobal.getConfig().getVerbosity() == 0);
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
                s.append(StringUtils.center(String.format("%d", PExGlobal.getTimelines().size()), 12));
                s.append(
                        StringUtils.center(
                                String.format(
                                        "%d (%.0f %% data)", getNumUnexploredChoices(), getUnexploredDataChoicesPercent()),
                                24));

                if (PExGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
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