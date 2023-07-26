package psym.runtime;

import java.io.PrintWriter;
import java.io.Serializable;
import java.io.StringWriter;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import lombok.Getter;
import lombok.Setter;
import psym.commandline.PSymConfiguration;
import psym.runtime.logger.PSymLogger;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Event;
import psym.runtime.machine.events.StateEvents;
import psym.runtime.scheduler.Scheduler;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceLearningStats;
import psym.runtime.scheduler.search.explicit.ExplicitSymmetryTracker;
import psym.runtime.scheduler.search.symbolic.SymbolicSymmetryTracker;
import psym.runtime.scheduler.search.symmetry.SymmetryTracker;
import psym.runtime.statistics.CoverageStats;

/**
 * Class containing global/shared data that is retained when resuming a run
 */
public class PSymGlobal implements Serializable {
    /**
     * Singleton object of the class
     */
    private static PSymGlobal PSymGlobal = null;

    /**
     * Global configuration
     */
    private static PSymConfiguration configuration = null;

    /**
     * Scheduler
     */
    private static Scheduler scheduler = null;

    /** Status of the run **/
    @Getter @Setter
    private static String status = "incomplete";

    /** Result of the run **/
    @Getter @Setter
    private static String result = "error";

    /**
     * Mapping of each machine's state with its corresponding event handlers
     */
    private final Map<String, StateEvents> allStateEvents = new HashMap<>();

    /**
     * Set of sync event names
     */
    private final Map<String, Set<String>> syncEvents = new HashMap<>();

    /**
     * Global coverage statistics
     */
    private final CoverageStats coverageStats = new CoverageStats();

    /**
     * Global choice feature statistics
     */
    private final ChoiceLearningStats choiceLearningStats = new ChoiceLearningStats();

    /**
     * Global symmetry tracker
     */
    private SymmetryTracker symmetryTracker = null;

    /**
     * Private constructor to enable singleton class object
     */
    private PSymGlobal() {}

    /**
     * Get/create the singleton class object
     */
    public static synchronized PSymGlobal getInstance() {
        if (PSymGlobal == null) {
            setInstance(new PSymGlobal());
        }
        return PSymGlobal;
    }

    /**
     * Set the global data singleton object after resuming a run
     *
     * @param rhs Singleton object to set to
     */
    public static void setInstance(PSymGlobal rhs) {
        PSymGlobal = rhs;
    }

    public static PSymConfiguration getConfiguration() {
        return getInstance().configuration;
    }
    public static void setConfiguration(PSymConfiguration config) {
        getInstance().configuration = config;
    }

    public static Scheduler getScheduler() {
        return getInstance().scheduler;
    }

    public static void setScheduler(Scheduler s) {
        getInstance().scheduler = s;
    }

    public static Map<String, StateEvents> getAllStateEvents() {
        return getInstance().allStateEvents;
    }

    public static void addSyncEvent(String machineName, String eventName) {
        Set<String> syncEvents =
            getInstance().syncEvents.computeIfAbsent(machineName, k -> new HashSet<>());
        syncEvents.add(eventName);
        getInstance().syncEvents.put(machineName, syncEvents);
    }

    public static boolean hasSyncEvent(Machine machine, Event event) {
        if (configuration.isAllowSyncEvents()) {
            if (event.toString().startsWith("sync_")) {
                return true;
            }
            Set<String> syncEvents = getInstance().syncEvents.get(machine.getName());
            if (syncEvents == null) {
                return false;
            }
            return syncEvents.contains(event.toString());
        } else {
            return false;
        }
    }

    public static CoverageStats getCoverage() {
        return getInstance().coverageStats;
    }

    public static ChoiceLearningStats getChoiceLearningStats() {
        return getInstance().choiceLearningStats;
    }

    public static SymmetryTracker getSymmetryTracker() {
        return getInstance().symmetryTracker;
    }

    public static void setSymmetryTracker(SymmetryTracker rhs) {
        getInstance().symmetryTracker = rhs;
    }

    public static void initializeSymmetryTracker(boolean isSymbolic) {
        getInstance().symmetryTracker =
            isSymbolic ? new SymbolicSymmetryTracker() : new ExplicitSymmetryTracker();
    }

    public static void printStackTrace(Exception e, boolean stderrOnly) {
        if (stderrOnly) {
            System.err.println("... Stack trace:");
            e.printStackTrace(System.err);
        } else {
            StringWriter sw = new StringWriter();
            PrintWriter pw = new PrintWriter(sw);
            e.printStackTrace(pw);

            PSymLogger.info("... Stack trace:");
            PSymLogger.info(sw.toString());
        }
    }
}
