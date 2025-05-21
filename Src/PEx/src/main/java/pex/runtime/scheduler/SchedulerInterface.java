package pex.runtime.scheduler;

import pex.values.*;

import java.io.Serializable;
import java.util.concurrent.TimeoutException;

/**
 * Represents a scheduler interface
 */
public interface SchedulerInterface extends Serializable {

    /**
     * Perform the search
     */
    void run() throws TimeoutException, InterruptedException;

    /**
     * Return a random PBool based on the search and strategy.
     *
     * @param loc location in P model
     * @return a boolean choice.
     */
    PBool getRandomBool(String loc);

    /**
     * Return a random PInt (within a bound) based on the search and strategy.
     *
     * @param loc   location in P model
     * @param bound upper bound (exclusive) on the integer.
     * @return a integer
     */
    PInt getRandomInt(String loc, PInt bound);

    /**
     * Return a random element of a PSeq based on the search and strategy.
     *
     * @param loc      location in P model
     * @param elements list to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(String loc, PSeq elements);

    /**
     * Return a random element of a PSet based on the search and strategy.
     *
     * @param loc      location in P model
     * @param elements set to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(String loc, PSet elements);

    /**
     * Return a random key of a PMap based on the search and strategy.
     *
     * @param loc      location in P model
     * @param elements map to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(String loc, PMap elements);

}
