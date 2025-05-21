package pex.runtime;

import pex.runtime.machine.PTestDriver;

import java.io.Serializable;

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
