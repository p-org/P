package pcover.runtime;

import lombok.Getter;
import lombok.Setter;
import pcover.commandline.PCoverConfig;
import pcover.runtime.machine.events.StateEvents;
import pcover.runtime.scheduler.Scheduler;

import java.io.Serializable;
import java.util.HashMap;
import java.util.Map;

/**
 * Represents global data structures represented with a singleton class
 */
public class PCoverGlobal implements Serializable {
    private static PCoverGlobal PCoverGlobal = null;
    /**
     * Singleton object of the class
     **/
    private static PCoverConfig config = null;
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
    private PCoverGlobal() {
    }

    /**
     * Get/create the singleton class object
     */
    public static synchronized PCoverGlobal getInstance() {
        if (PCoverGlobal == null) {
            setInstance(new PCoverGlobal());
        }
        return PCoverGlobal;
    }

    /**
     * Set the global data singleton object after resuming a run
     *
     * @param rhs Singleton object to set to
     */
    public static void setInstance(PCoverGlobal rhs) {
        PCoverGlobal = rhs;
    }

    /**
     * Get global config
     *
     * @return PCoverConfig object
     */
    public static PCoverConfig getConfig() {
        getInstance();
        return config;
    }

    /**
     * Set global config
     *
     * @param config PCoverConfig object
     */
    public static void setConfig(PCoverConfig config) {
        getInstance();
        pcover.runtime.PCoverGlobal.config = config;
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