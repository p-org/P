package psym.runtime;

import psym.runtime.machine.events.StateEvents;
import psym.runtime.scheduler.explicit.choiceorchestration.ChoiceLearningStats;
import psym.runtime.scheduler.symmetry.SymmetryTracker;
import psym.runtime.statistics.CoverageStats;

import java.io.Serializable;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

/**
 * Class containing global/shared data that is retained when resuming a run
 */
public class GlobalData implements Serializable {
    /**
     * Singleton object of the class
     */
    private static GlobalData globalData = null;

    /**
     * Mapping of each machine's state with its corresponding event handlers
     */
    private final Map<String, StateEvents> allStateEvents = new HashMap<>();

    /**
     * Set of sync event names
     */
    private final Set<String> syncEvents = new HashSet<>();

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
    private SymmetryTracker symmetryTracker = new SymmetryTracker();

    /**
     * Private constructor to enable singleton class object
     */
    private GlobalData() {
    }

    /**
     * Get/create the singleton class object
     */
    public static synchronized GlobalData getInstance() {
        if (globalData == null) {
            setInstance(new GlobalData());
        }
        return globalData;
    }

    /**
     * Set the global data singleton object after resuming a run
     *
     * @param rhs Singleton object to set to
     */
    public static void setInstance(GlobalData rhs) {
        globalData = rhs;
    }

    public static Map<String, StateEvents> getAllStateEvents() {
        return getInstance().allStateEvents;
    }

    public static Set<String> getSyncEvents() {
        return getInstance().syncEvents;
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
}
