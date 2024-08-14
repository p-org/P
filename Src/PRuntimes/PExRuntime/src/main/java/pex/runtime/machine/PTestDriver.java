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
    public final List<PMonitor> monitorList;
    public final Map<PEvent, List<PMonitor>> observerMap;
    public PMachine mainMachine;

    /**
     * Test driver constructor
     */
    public PTestDriver() {
        this.mainMachine = null;
        this.monitorList = new ArrayList<>();
        this.observerMap = new HashMap<>();
        configure();
    }

    /**
     * Get the start/main machine of this test driver.
     *
     * @return the start/main machine of this test driver.
     */
    public PMachine getStart() {
        return mainMachine;
    }

    /**
     * Get the list of monitors of this test driver.
     *
     * @return List of monitors
     */
    public List<PMonitor> getMonitors() {
        return monitorList;
    }

    /**
     * Get all event monitors mapping of this test driver.
     *
     * @return Map from event to list of monitors listening/observing that event
     */
    public Map<PEvent, List<PMonitor>> getListeners() {
        return observerMap;
    }

    /**
     * Configure this test driver
     */
    public abstract void configure();
}