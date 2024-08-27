package pex.runtime.machine;

import pex.values.PEvent;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Represents the base class for a P test driver.
 */
public abstract class PTestDriver implements Serializable {
    // TODO: pex parallel - make fields thread safe
    public final Map<Class<? extends PMachine>, Class<? extends PMachine>> interfaceMap;
    public final List<Class<? extends PMachine>> monitorList;
    public final Map<PEvent, List<Class<? extends PMachine>>> observerMap;
    public Class<? extends PMachine> mainMachine;

    /**
     * Test driver constructor
     */
    public PTestDriver() {
        this.mainMachine = null;
        this.monitorList = new ArrayList<>();
        this.observerMap = new HashMap<>();
        this.interfaceMap = new HashMap<>();
        configure();
    }

    /**
     * Get the start/main machine of this test driver.
     *
     * @return the start/main machine of this test driver.
     */
    public Class<? extends PMachine> getStart() {
        return mainMachine;
    }

    /**
     * Get the list of monitors of this test driver.
     *
     * @return List of monitors
     */
    public List<Class<? extends PMachine>> getMonitors() {
        return monitorList;
    }

    /**
     * Get all event monitors mapping of this test driver.
     *
     * @return Map from event to list of monitors listening/observing that event
     */
    public Map<PEvent, List<Class<? extends PMachine>>> getListeners() {
        return observerMap;
    }

    /**
     * Configure this test driver
     */
    public abstract void configure();
}