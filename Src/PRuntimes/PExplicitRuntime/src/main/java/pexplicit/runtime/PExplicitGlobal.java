package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.machine.events.StateEvents;
import pexplicit.runtime.scheduler.Scheduler;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal implements Serializable {
    private static PExplicitGlobal PExplicitGlobal = null;
    /**
     * Singleton object of the class
     **/
    private static PExplicitConfig config = null;
    /**
     * Global configuration
     **/
    private static Scheduler scheduler = null;
    /**
     * Scheduler
     **/
    @Getter
    @Setter
    private static String status = "incomplete";
    /**
     * Status of the run
     **/
    @Getter
    @Setter
    private static String result = "error";
    /**
     * Result of the run
     **/
    private final Map<String, StateEvents> allStateEvents = new HashMap<>();    /* Mapping of each machine's state with its corresponding event handlers **/

    /**
     * Private constructor to enable singleton class object
     */
    private PExplicitGlobal() {
    }

    /**
     * Get/create the singleton class object
     */
    public static synchronized PExplicitGlobal getInstance() {
        if (PExplicitGlobal == null) {
            setInstance(new PExplicitGlobal());
        }
        return PExplicitGlobal;
    }

    /**
     * Set the global data singleton object after resuming a run
     *
     * @param rhs Singleton object to set to
     */
    public static void setInstance(PExplicitGlobal rhs) {
        PExplicitGlobal = rhs;
    }

    /**
     * Get global config
     *
     * @return PExplicitConfig object
     */
    public static PExplicitConfig getConfig() {
        getInstance();
        return config;
    }

    /**
     * Set global config
     *
     * @param config PExplicitConfig object
     */
    public static void setConfig(PExplicitConfig config) {
        getInstance();
        pexplicit.runtime.PExplicitGlobal.config = config;
    }

    /**
     * Get scheduler
     *
     * @return Scheduler object
     */
    public static Scheduler getScheduler() {
        getInstance();
        return scheduler;
    }

    /**
     * Set scheduler
     *
     * @param sch Scheduler object
     */
    public static void setScheduler(Scheduler sch) {
        getInstance();
        scheduler = sch;
    }

    /**
     * Get machine state to state events mapping
     *
     * @return map from machine state name to StateEvents object
     */
    public static Map<String, StateEvents> getAllStateEvents() {
        return getInstance().allStateEvents;
    }
}