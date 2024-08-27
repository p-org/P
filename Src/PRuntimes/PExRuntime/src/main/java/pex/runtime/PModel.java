package pex.runtime;

import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMonitor;
import pex.runtime.machine.PTestDriver;
import pex.values.PEvent;

import java.io.Serializable;
import java.util.List;
import java.util.Map;

/**
 * Interface of a PEx IR model/program.
 */
public interface PModel extends Serializable {
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
