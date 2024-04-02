package pexplicit.runtime;

import lombok.Getter;
import lombok.Setter;
import pexplicit.commandline.PExplicitConfig;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.runtime.machine.events.StateEvents;
import pexplicit.runtime.scheduler.Scheduler;
import pexplicit.values.PEvent;

import java.io.Serializable;
import java.util.*;

/**
 * Represents global data structures represented with a singleton class
 */
public class PExplicitGlobal {
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
    private static PExplicitConfig config = null;

    /**
     * Scheduler
     **/
    @Getter
    @Setter
    private static Scheduler scheduler = null;

    /**
     * Status of the run
     **/
    @Getter
    @Setter
    private static String status = "incomplete";

    /**
     * Result of the run
     **/
    @Getter
    @Setter
    private static String result = "error";
}