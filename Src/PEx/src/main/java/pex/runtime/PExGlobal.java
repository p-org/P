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
 * Manages global state and shared resources for the PEx runtime.
 * This class provides centralized access to runtime state, configuration,
 * schedulers, and statistics tracking.
 */
public class PExGlobal {
    // State tracking collections
    /** Map from state hash to schedulerId-iteration when first visited */
    @Getter
    private static final Map<Object, String> stateCache = new ConcurrentHashMap<>();
    
    /** Set of unique execution timelines */
    @Getter
    private static final Set<Object> timelines = ConcurrentHashMap.newKeySet();
    
    // Task tracking collections
    /** Map of all search tasks by ID */
    @Getter
    private static final Map<Integer, SearchTask> allTasks = new ConcurrentHashMap<>();
    
    /** Set of tasks that are waiting to be executed */
    @Getter
    private static final Set<SearchTask> pendingTasks = ConcurrentHashMap.newKeySet();
    
    /** Set of tasks that have completed execution */
    @Getter
    private static final Set<SearchTask> finishedTasks = ConcurrentHashMap.newKeySet();
    
    /** Set of tasks currently being executed */
    @Getter
    private static final Set<SearchTask> runningTasks = ConcurrentHashMap.newKeySet();
    
    // Scheduler management
    /** Map of explicit search schedulers by ID */
    @Getter
    private static final Map<Integer, ExplicitSearchScheduler> searchSchedulers = new ConcurrentHashMap<>();
    
    /** Map of thread IDs to scheduler IDs */
    @Getter
    private static final Map<Long, Integer> threadToSchedulerId = new ConcurrentHashMap<>();
    
    /** Map of scheduler instances to global machine IDs */
    private static final Map<Scheduler, Integer> globalMachineId = new ConcurrentHashMap<>();
    
    /** Map of scheduler instances to global monitor IDs */
    private static final Map<Scheduler, Integer> globalMonitorId = new ConcurrentHashMap<>();
    
    // Core runtime components
    /** The active P model */
    @Getter
    @Setter
    private static PModel model = null;
    
    /** Global runtime configuration */
    @Getter
    @Setter
    private static PExConfig config = null;
    
    /** Replay scheduler for bug reproduction */
    @Setter
    private static ReplayScheduler replayScheduler = null;
    
    /** Current execution status */
    @Getter
    @Setter
    private static STATUS status = STATUS.INCOMPLETE;
    
    /** Result of the current run */
    @Getter
    @Setter
    private static String result = "error";
    
    /** Component for making exploration choices */
    @Getter
    private static ChoiceSelector choiceSelector = null;
    
    /** Time of last status report */
    private static Instant lastReportTime = Instant.now();

    /**
     * Adds a search scheduler to the global registry
     *
     * @param scheduler The scheduler to add
     */
    public static void addSearchScheduler(ExplicitSearchScheduler scheduler) {
        searchSchedulers.put(scheduler.getSchedulerId(), scheduler);
    }

    /**
     * Registers the current thread with a specific scheduler
     *
     * @param schedulerId ID of the scheduler to register with the current thread
     * @throws AssertionError if the scheduler ID is not found or if the thread is already registered
     */
    public static void registerSearchScheduler(int schedulerId) {
        assert searchSchedulers.containsKey(schedulerId) : 
            "Scheduler ID " + schedulerId + " not found in registry";

        long threadId = Thread.currentThread().getId();
        assert !threadToSchedulerId.containsKey(threadId) : 
            "Thread " + threadId + " already registered with a scheduler";
            
        threadToSchedulerId.put(threadId, schedulerId);
    }

    /**
     * Gets the scheduler associated with the current thread
     *
     * @return The scheduler associated with the current thread, or the replay scheduler if active
     * @throws AssertionError if no scheduler is associated with the current thread
     */
    public static Scheduler getScheduler() {
        // If replay scheduler is active, it takes precedence
        if (replayScheduler != null) {
            return replayScheduler;
        }
        
        // Otherwise, find the scheduler for the current thread
        long threadId = Thread.currentThread().getId();
        Integer schedulerId = threadToSchedulerId.get(threadId);
        assert schedulerId != null : "No scheduler registered for thread " + threadId;
        
        return searchSchedulers.get(schedulerId);
    }

    /**
     * Gets the global machine ID associated with the current scheduler
     *
     * @return The global machine ID for the current scheduler
     */
    public static int getGlobalMachineId() {
        Scheduler scheduler = getScheduler();
        return globalMachineId.computeIfAbsent(scheduler, s -> 1);
    }

    /**
     * Sets the global machine ID for the current scheduler
     *
     * @param id The machine ID to set
     */
    public static void setGlobalMachineId(int id) {
        globalMachineId.put(getScheduler(), id);
    }

    /**
     * Gets the global monitor ID associated with the current scheduler
     *
     * @return The global monitor ID for the current scheduler
     */
    public static int getGlobalMonitorId() {
        Scheduler scheduler = getScheduler();
        return globalMonitorId.computeIfAbsent(scheduler, s -> -1);
    }

    /**
     * Sets the global monitor ID for the current scheduler
     *
     * @param id The monitor ID to set
     */
    public static void setGlobalMonitorId(int id) {
        globalMonitorId.put(getScheduler(), id);
    }

    /**
     * Initializes the choice selector based on configuration
     * 
     * @throws RuntimeException if the choice selector mode is invalid
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
                throw new RuntimeException("Unrecognized choice orchestrator: " + 
                    config.getChoiceSelectorMode());
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

    /**
     * Get the total number of unexplored choices across all tasks
     *
     * @return Total count of unexplored choices
     */
    static int getNumUnexploredChoices() {
        int pendingChoices = getNumPendingChoices();
        
        // Add choices from currently running tasks
        int runningTaskChoices = searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getSearchStrategy().getCurrTask().getCurrentNumUnexploredChoices())
            .sum();
            
        return pendingChoices + runningTaskChoices;
    }

    /**
     * Get the total number of unexplored data choices across all tasks
     *
     * @return Total count of unexplored data choices
     */
    static int getNumUnexploredDataChoices() {
        int pendingDataChoices = getNumPendingDataChoices();
        
        // Add data choices from currently running tasks
        int runningTaskDataChoices = searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getSearchStrategy().getCurrTask().getCurrentNumUnexploredDataChoices())
            .sum();
            
        return pendingDataChoices + runningTaskDataChoices;
    }

    /**
     * Calculate the percentage of unexplored choices that are data choices
     *
     * @return Percentage of unexplored choices that are data choices (0-100)
     */
    static double getUnexploredDataChoicesPercent() {
        int totalUnexplored = getNumUnexploredChoices();
        if (totalUnexplored == 0) {
            return 0.0;
        }

        int dataChoices = getNumUnexploredDataChoices();
        return (dataChoices * 100.0) / totalUnexplored;
    }

    /**
     * Update the result status based on current execution state
     * Does nothing if a bug has already been found
     */
    public static void updateResult() {
        // Don't update result if a bug was found
        if (status == STATUS.BUG_FOUND) {
            return;
        }

        int maxStepBound = config.getMaxStepBound();
        int currentMaxSteps = getMaxSteps();
        int unexploredChoices = getNumUnexploredChoices();
        String resultString;
        
        // Generate result string based on step bounds and unexplored choices
        if (currentMaxSteps < maxStepBound) {
            // Below step bound
            if (unexploredChoices == 0) {
                resultString = "correct for any depth";
            } else {
                resultString = String.format("partially correct with %,d choices remaining", 
                    unexploredChoices);
            }
        } else {
            // At or exceeding step bound
            if (unexploredChoices == 0) {
                resultString = String.format("correct up to step %,d", currentMaxSteps);
            } else {
                resultString = String.format(
                    "partially correct up to step %,d with %,d choices remaining", 
                    currentMaxSteps, unexploredChoices);
            }
        }
        
        result = resultString;
    }

    /**
     * Records execution statistics to the stats writer
     * This method logs various metrics about the current execution state
     */
    public static void recordStats() {
        // Log basic schedule and state information
        StatWriter.log("#-schedules", String.format("%d", getTotalSchedules()));
        StatWriter.log("#-timelines", String.format("%d", timelines.size()));
        
        // Log state caching information if enabled
        if (config.getStateCachingMode() != StateCachingMode.None) {
            StatWriter.log("#-states", String.format("%d", getTotalStates()));
            StatWriter.log("#-distinct-states", String.format("%d", stateCache.size()));
        }
        
        // Log step statistics
        StatWriter.log("steps-min", String.format("%d", getMinSteps()));
        
        // Avoid division by zero
        int totalSchedules = getTotalSchedules();
        long avgSteps = totalSchedules > 0 ? getTotalSteps() / totalSchedules : 0;
        StatWriter.log("steps-avg", String.format("%d", avgSteps));
        
        // Log choice exploration statistics
        StatWriter.log("#-choices-unexplored", String.format("%d", getNumUnexploredChoices()));
        StatWriter.log("%-choices-unexplored-data", String.format("%.1f", getUnexploredDataChoicesPercent()));
        
        // Log task statistics
        StatWriter.log("#-tasks-finished", String.format("%d", finishedTasks.size()));
        StatWriter.log("#-tasks-pending", String.format("%d", pendingTasks.size()));
        
        // Log Q-learning statistics
        StatWriter.log("ql-#-states", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumStates()));
        StatWriter.log("ql-#-actions", String.format("%d", ChoiceSelectorQL.getChoiceQL().getNumActions()));
    }

    /**
     * Print detailed status information to the scratch logger
     * 
     * @param elapsedRuntime Current runtime in seconds
     */
    static void printCurrentStatus(double elapsedRuntime) {
        StringBuilder status = new StringBuilder("--------------------");
        status.append(String.format("\n    Status after %.2f seconds:", elapsedRuntime));
        status.append(String.format("\n      Memory:           %.2f MB", MemoryMonitor.getMemSpent()));
        status.append(String.format("\n      Schedules:        %d", getTotalSchedules()));
        
        // Add task statistics
        status.append(String.format("\n      RunningTasks:     %d", runningTasks.size()));
        status.append(String.format("\n      FinishedTasks:    %d", finishedTasks.size()));
        status.append(String.format("\n      PendingTasks:     %d", pendingTasks.size()));
        
        // Add timeline statistics
        status.append(String.format("\n      Timelines:        %d", timelines.size()));
        
        // Add state information if state caching is enabled
        if (config.getStateCachingMode() != StateCachingMode.None) {
            status.append(String.format("\n      States:           %d", getTotalStates()));
            status.append(String.format("\n      DistinctStates:   %d", stateCache.size()));
        }
        
        ScratchLogger.log(status.toString());
    }

    /**
     * Print the progress table header to the console
     */
    public static void printProgressHeader() {
        StringBuilder header = new StringBuilder(100);
        
        // Add column headers
        header.append(StringUtils.center("Time", 11));
        header.append(StringUtils.center("Memory", 9));
        header.append(StringUtils.center("Tasks (run/fin/pen)", 24));
        header.append(StringUtils.center("Schedules", 12));
        header.append(StringUtils.center("Timelines", 12));
        
        // Add state information column if state caching is enabled
        if (config.getStateCachingMode() != StateCachingMode.None) {
            header.append(StringUtils.center("States", 12));
        }

        // Print the header
        System.out.println("--------------------");
        System.out.println(header);
    }

    /**
     * Print the progress table footer to the console
     */
    public static void printProgressFooter() {
        System.out.println();
    }

    /**
     * Print progress information to the console
     * 
     * @param forcePrint If true, print regardless of time since last print;
     *                  if false, only print if sufficient time has elapsed
     */
    public static void printProgress(boolean forcePrint) {
        // Check if we should print based on timer or force print
        if (forcePrint || (TimeMonitor.findInterval(lastReportTime) > 10)) {
            // Update last report time
            lastReportTime = Instant.now();
            
            // Get current runtime
            double elapsedRuntime = TimeMonitor.getRuntime();
            
            // Print detailed status to log
            printCurrentStatus(elapsedRuntime);
            
            // Format runtime as HH:MM:SS
            long runtimeMs = (long)(elapsedRuntime * 1000);
            String runtimeFormatted = String.format(
                "%02d:%02d:%02d",
                TimeUnit.MILLISECONDS.toHours(runtimeMs),
                TimeUnit.MILLISECONDS.toMinutes(runtimeMs) % TimeUnit.HOURS.toMinutes(1),
                TimeUnit.MILLISECONDS.toSeconds(runtimeMs) % TimeUnit.MINUTES.toSeconds(1)
            );

            // Build the progress line
            StringBuilder progress = new StringBuilder(100);
            progress.append('\r');
            progress.append(StringUtils.center(runtimeFormatted, 11));
            progress.append(StringUtils.center(String.format("%.1f GB", MemoryMonitor.getMemSpent() / 1024), 9));
            
            // Add task statistics
            progress.append(StringUtils.center(
                String.format("%,d / %,d / %,d", runningTasks.size(), finishedTasks.size(), pendingTasks.size()),
                24
            ));
            
            // Add schedule and timeline statistics
            progress.append(StringUtils.center(String.format("%,d", getTotalSchedules()), 12));
            progress.append(StringUtils.center(String.format("%,d", timelines.size()), 12));
            
            // Add state information if state caching is enabled
            if (config.getStateCachingMode() != StateCachingMode.None) {
                progress.append(StringUtils.center(String.format("%,d", stateCache.size()), 12));
            }

            // Print progress line
            System.out.print(progress);
        }
    }

    /**
     * Get the total number of schedules explored across all schedulers
     * 
     * @return Total schedule count
     */
    public static int getTotalSchedules() {
        return searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getStats().numSchedules)
            .sum();
    }

    /**
     * Get the minimum number of steps among all schedulers
     * 
     * @return Minimum step count, or -1 if no schedulers exist
     */
    public static int getMinSteps() {
        return searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getStats().minSteps)
            .min()
            .orElse(-1);
    }

    /**
     * Get the maximum number of steps among all schedulers
     * 
     * @return Maximum step count, or -1 if no schedulers exist
     */
    public static int getMaxSteps() {
        return searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getStats().maxSteps)
            .max()
            .orElse(-1);
    }

    /**
     * Get the total number of steps executed across all schedulers
     * 
     * @return Total step count
     */
    public static long getTotalSteps() {
        return searchSchedulers.values().stream()
            .mapToLong(sch -> sch.getStats().totalSteps)
            .sum();
    }

    /**
     * Get the total number of states explored across all schedulers
     * 
     * @return Total state count
     */
    public static int getTotalStates() {
        return searchSchedulers.values().stream()
            .mapToInt(sch -> sch.getStats().totalStates)
            .sum();
    }
}
