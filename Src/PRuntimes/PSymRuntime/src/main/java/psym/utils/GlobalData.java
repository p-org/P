package psym.utils;

import psym.runtime.StateEvents;
import psym.runtime.scheduler.choiceorchestration.ChoiceLearningStats;
import psym.runtime.statistics.CoverageStats;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;

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
    public Map<String, StateEvents> allStateEvents;

    /**
     * Global coverage statistics
     */
    public CoverageStats coverageStats;

    /**
     * Global choice feature statistics
     */
    public ChoiceLearningStats choiceLearningStats;

    /**
     * Private constructor to enable singleton class object
     */
    private GlobalData() {
        allStateEvents = new HashMap<>();
        coverageStats = new CoverageStats();
        choiceLearningStats = new ChoiceLearningStats();
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
     * Get coverage statistics
     * @return CoverageStats object
     */
    public static CoverageStats getCoverage() {
        return getInstance().coverageStats;
    }

    /**
     * Get choice feature statistics
     * @return ChoiceFeatureStats object
     */
    public static ChoiceLearningStats getChoiceLearningStats() {
        return getInstance().choiceLearningStats;
    }

    /**
     * Set the global data singleton object after resuming a run
     * @param rhs Singleton object to set to
     */
    public static void setInstance(GlobalData rhs) {
        globalData = rhs;
    }

}
