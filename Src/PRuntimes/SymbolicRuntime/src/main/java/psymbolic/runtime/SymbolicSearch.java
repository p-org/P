package psymbolic.runtime;

import psymbolic.valuesummary.ListVS;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.ValueSummary;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.Set;

/** Search interface for exploring different schedules */
public interface SymbolicSearch {
    /** Specify what the max depth should be before considering an error to have been reached.
     *
     * @param errorDepth the error depth
     */
     void setErrorDepth(int errorDepth);

    /** Specify what the max depth should be.
     *
     * @param maxDepth the maximum depth that should be searched.
     */
    void setMaxDepth(int maxDepth);

    /** Perform the Search
     *
     * @param target The target machine of the program
     */
    void doSearch (Machine target);

    /** Return the next integer (within a bound) based on the search and strategy.
     *
     * @param bound upper bound (exclusive) on the integer.
     * @return a integer
     */
    PrimVS<Integer> getNextInteger(PrimVS<Integer> bound, Bdd pc);

    /** Return the next boolean based on the search and strategy.
     *
     * @return a boolean choice.
     */
    PrimVS<Boolean> getNextBoolean(Bdd pc);

    /** Return the next element of a finite set based on the search and strategy.
     *
     * @param s set to choose from
     * @return a integer
     */
    ValueSummary getNextElement(ListVS<? extends ValueSummary> s, Bdd pc);

}
