package psymbolic.runtime;

import psymbolic.commandline.Program;
import psymbolic.valuesummary.*;

import java.io.Serializable;

/** Search interface for exploring different schedules */
public interface SymbolicSearch extends Serializable {

    /** Perform the Search
     *
     * @param p The program to run the search on
     */
    void doSearch (Program p);

    /** Resume the Search
     *
     * @param p The program to resume the search on
     */
    void resumeSearch (Program p);

    /** Return the next integer (within a bound) based on the search and strategy.
     *
     * @param bound upper bound (exclusive) on the integer.
     * @return a integer
     */
    PrimitiveVS<Integer> getNextInteger(PrimitiveVS<Integer> bound, Guard pc);

    /** Return the next boolean based on the search and strategy.
     *
     * @return a boolean choice.
     */
    PrimitiveVS<Boolean> getNextBoolean(Guard pc);

    /** Return the next element of a finite set based on the search and strategy.
     *
     * @param s list to choose from
     * @return a integer
     */
    ValueSummary getNextElement(ListVS<? extends ValueSummary> s, Guard pc);

    /** Return the next element of a finite set based on the search and strategy.
     *
     * @param s set to choose from
     * @return a integer
     */
    ValueSummary getNextElement(SetVS<? extends ValueSummary> s, Guard pc);

}
