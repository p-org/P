package psym.runtime.scheduler;

import java.io.Serializable;
import java.util.concurrent.TimeoutException;
import psym.valuesummary.*;

/**
 * Scheduler interface for exploring different schedules
 */
public interface SchedulerInterface extends Serializable {

    /**
     * Perform the Search
     */
    void doSearch() throws TimeoutException, InterruptedException;

    /**
     * Resume the Search
     */
    void resumeSearch() throws TimeoutException, InterruptedException;

    /**
     * Return the next integer (within a bound) based on the search and strategy.
     *
     * @param bound upper bound (exclusive) on the integer.
     * @return a integer
     */
    PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc);

    /**
     * Return the next boolean based on the search and strategy.
     *
     * @return a boolean choice.
     */
    PrimitiveVS<Boolean> getNextBoolean(Guard pc);

    /**
     * Return the next element of a finite set based on the search and strategy.
     *
     * @param s list to choose from
     * @return a integer
     */
    ValueSummary getNextElement(ListVS<? extends ValueSummary> s, Guard pc);

    /**
     * Return the next element of a finite set based on the search and strategy.
     *
     * @param s set to choose from
     * @return a integer
     */
    ValueSummary getNextElement(SetVS<? extends ValueSummary> s, Guard pc);

    /**
     * Return the next key of a finite map based on the search and strategy.
     *
     * @param s map to choose from
     * @return a integer
     */
    ValueSummary getNextElement(MapVS<?, ? extends ValueSummary, ? extends ValueSummary> s, Guard pc);

}
