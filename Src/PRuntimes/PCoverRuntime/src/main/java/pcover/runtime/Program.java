package pcover.runtime;

import pcover.runtime.machine.Machine;
import pcover.runtime.machine.Monitor;
import pcover.runtime.machine.PTestDriver;
import pcover.values.PEvent;

import java.io.Serializable;
import java.util.List;
import java.util.Map;

/**
 * Interface of a PCover IR model/program.
 */
public interface Program extends Serializable {
    /**
     * Get the start/main machine
     * @return Machine
     */
    Machine getStart();

    /**
     * Get the mapping from events to monitors listening/observing an event
     * @return
     */
    Map<PEvent, List<Monitor>> getListeners();

    /**
     * Get the list of monitors
     * @return List of monitors
     */
    List<Monitor> getMonitors();

    /**
     * Get the test driver
     * @return PTestDriver object
     */
    PTestDriver getTestDriver();

    /**
     * Set the test driver
     * @param driver Test driver to set to
     */
    void setTestDriver(PTestDriver driver);
}
