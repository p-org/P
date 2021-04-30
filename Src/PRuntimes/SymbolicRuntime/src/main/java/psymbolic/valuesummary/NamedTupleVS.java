package psymbolic.valuesummary;

package symbolicp.vs;

import p.runtime.values.PString;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;

/** Class for named tuple value summaries */
public class NamedTupleVS implements ValueSummary<NamedTupleVS> {
    /** Mapping from names of the fields to their index in the underlying representation */
    private final Map<String, Integer> names;
    /** Underlying representation as a TupleVS */
    private final TupleVS tuple;

    private NamedTupleVS(Map<String, Integer> names, TupleVS tuple) {
        this.names = names;
        this.tuple = tuple;
    }

    public NamedTupleVS (NamedTupleVS namedTuple) {
        this.names = new HashMap<>(namedTuple.names);
        this.tuple = new TupleVS(namedTuple.tuple);
    }

    /** Get the names of the NamedTupleVS fields
     * @return Array containing the names of the NamedTupleVS fields */
    public String[] getNames() {
        return (String[]) names.keySet().toArray();
    }

    /** Make a new NamedTupleVS with the provided names and fields
     * @param namesAndFields Alternating String and ValueSummary values where the Strings give the field names
     */
    public NamedTupleVS(Object... namesAndFields) {
        names = new HashMap<>();
        ValueSummary[] vs = new ValueSummary[namesAndFields.length/ 2];
        for (int i = 0; i < namesAndFields.length; i += 2) {
            String name = (String)namesAndFields[i];
            vs[i / 2] = (ValueSummary)namesAndFields[i + 1];
            names.put(name, i / 2);
        }
        tuple = new TupleVS(vs);
    }

    /** Get the value for a particular field
     * @param name The name of the field
     * @return The value
     */
    public ValueSummary getField(String name) {
        return tuple.getField(names.get(name));
    }

    /** Get the value for a particular field
     * @param name The name of the field
     * @return The value
     */
    public ValueSummary getField(PString name) {
        return tuple.getField(names.get(name.getValue()));
    }

    /** Set the value for a particular field
     * @param name The field name
     * @param val The value to set the specified field to
     * @return The result of updating the field
     */
    public NamedTupleVS setField(String name, ValueSummary val) {
        return new NamedTupleVS(names, tuple.setField(names.get(name), val));
    }

    /** Set the value for a particular field
     * @param name The field name
     * @param val The value to set the specified field to
     * @return The result of updating the field
     */
    public NamedTupleVS setField(PString name, ValueSummary val) {
        return new NamedTupleVS(names, tuple.setField(names.get(name.getValue()), val));
    }

    @Override
    public boolean isEmptyVS() {
        return tuple.isEmptyVS();
    }

    @Override
    public NamedTupleVS guard(Bdd guard) {
        return new NamedTupleVS(names, tuple.guard(guard));
    }

    @Override
    public NamedTupleVS merge(Iterable<NamedTupleVS> summaries) {
        final List<TupleVS> tuples = new ArrayList<TupleVS>();

        for (NamedTupleVS summary : summaries) {
            tuples.add(summary.tuple);
        }

        return new NamedTupleVS(names, tuple.merge(tuples));
    }

    @Override
    public NamedTupleVS merge(NamedTupleVS summaries) {
        return merge(Collections.singletonList(summaries));
    }

    @Override
    public NamedTupleVS update(Bdd guard, NamedTupleVS update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(NamedTupleVS cmp, Bdd pc) {
        if (names.equals(cmp.names)) {
            return new PrimVS<>(false).guard(pc);
        }
        return tuple.symbolicEquals(cmp.tuple, pc);
    }

    @Override
    public Bdd getUniverse() {
        return tuple.getUniverse();
    }
}
