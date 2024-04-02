package pexplicit.runtime.scheduler;

import pexplicit.values.*;

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
     * @return a boolean choice.
     */
    PBool getRandomBool();

    /**
     * Return a random PInt (within a bound) based on the search and strategy.
     *
     * @param bound upper bound (exclusive) on the integer.
     * @return a integer
     */
    PInt getRandomInt(PInt bound);

    /**
     * Return a random element of a PSeq based on the search and strategy.
     *
     * @param elements list to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(PSeq elements);

    /**
     * Return a random element of a PSet based on the search and strategy.
     *
     * @param elements set to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(PSet elements);

    /**
     * Return a random key of a PMap based on the search and strategy.
     *
     * @param elements map to choose from
     * @return a integer
     */
    PValue<?> getRandomEntry(PMap elements);

}
