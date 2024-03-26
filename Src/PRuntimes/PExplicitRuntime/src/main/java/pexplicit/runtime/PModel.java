package pexplicit.runtime;

import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.runtime.machine.PTestDriver;
import pexplicit.values.PEvent;

import java.io.Serializable;
import java.util.List;
import java.util.Map;

/**
 * Interface of a PExplicit IR model/program.
 */
public interface PModel extends Serializable {
    /**
     * Get the start/main machine
     *
     * @return Machine
     */
    PMachine getStart();

    /**
     * Get the mapping from events to monitors listening/observing an event
     *
     * @return
     */
    Map<PEvent, List<PMonitor>> getListeners();

    /**
     * Get the list of monitors
     *
     * @return List of monitors
     */
    List<PMonitor> getMonitors();

    /**
     * Get the test driver
     *
     * @return PTestDriver object
     */
    PTestDriver getTestDriver();

    /**
     * Set the test driver
     *
     * @param driver Test driver to set to
     */
    void setTestDriver(PTestDriver driver);
}
