package pex.runtime.machine;

import pex.values.Event;

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
    public final Map<Event, List<Class<? extends PMachine>>> observerMap;
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
     * Get test driver name
     */
    public static String getTestName(Class<? extends PTestDriver> td) {
        String result = td.getSimpleName();
        if (result.startsWith("test_"))
            return result.substring("test_".length());
        return result;
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
    public Map<Event, List<Class<? extends PMachine>>> getListeners() {
        return observerMap;
    }

    /**
     * Configure this test driver
     */
    public abstract void configure();
}