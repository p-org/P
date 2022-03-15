package psymbolic.valuesummary;

import p.runtime.values.PString;

import java.util.*;

/** Class for named tuple value summaries */
public class NamedTupleVS implements ValueSummary<NamedTupleVS> {
    /** List of names of the fields in the declared order*/
    private final List<String> names;
    /** Underlying representation as a TupleVS */
    private final TupleVS tuple;

    private NamedTupleVS(List<String> names, TupleVS tuple) {
        this.names = names;
        this.tuple = tuple;
    }

    public NamedTupleVS (NamedTupleVS namedTuple) {
        this.names = new ArrayList<>(namedTuple.names);
        this.tuple = new TupleVS(namedTuple.tuple);
    }

    /** Get the names of the NamedTupleVS fields
     * @return Array containing the names of the NamedTupleVS fields */
    public String[] getNames() {
        return names.toArray(new String[names.size()]);
    }

    /** Make a new NamedTupleVS with the provided names and fields
     * @param namesAndFields Alternating String and ValueSummary values where the Strings give the field names
     */
    public NamedTupleVS(Object... namesAndFields) {
        names = new ArrayList<>();
        ValueSummary<?>[] vs = new ValueSummary[namesAndFields.length/ 2];
        for (int i = 0; i < namesAndFields.length; i += 2) {
            vs[i / 2] = (ValueSummary<?>)namesAndFields[i + 1];
            names.add((String)namesAndFields[i]);
        }
        tuple = new TupleVS(vs);
    }

    /** Get the value for a particular field
     * @param name The name of the field
     * @return The value
     */
    public ValueSummary<?> getField(String name) {
        return tuple.getField(names.indexOf(name));
    }

    /** Get the value for a particular field
     * @param name The name of the field
     * @return The value
     */
    public ValueSummary<?> getField(PString name) {
        return tuple.getField(names.indexOf(name.getValue()));
    }

    /** Set the value for a particular field
     * @param name The field name
     * @param val The value to set the specified field to
     * @return The result of updating the field
     */
    public NamedTupleVS setField(String name, ValueSummary<?> val) {
        return new NamedTupleVS(names, tuple.setField(names.indexOf(name), val));
    }

    /** Set the value for a particular field
     * @param name The field name
     * @param val The value to set the specified field to
     * @return The result of updating the field
     */
    public NamedTupleVS setField(PString name, ValueSummary<?> val) {
        return new NamedTupleVS(names, tuple.setField(names.indexOf(name.toString()), val));
    }

    @Override
    public boolean isEmptyVS() {
        return tuple.isEmptyVS();
    }

    @Override
    public NamedTupleVS restrict(Guard guard) {
        return new NamedTupleVS(names, tuple.restrict(guard));
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
    public NamedTupleVS combineVals(NamedTupleVS other) {
        return new NamedTupleVS(this.names, this.tuple.combineVals(other.tuple));
    }

    @Override
    public NamedTupleVS updateUnderGuard(Guard guard, NamedTupleVS update) {
        return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));//.combineVals(this);
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(NamedTupleVS cmp, Guard pc) {
        if (!Arrays.deepEquals(names.toArray(), cmp.names.toArray())) {
            // TODO: raise an exception checking equality of two incompatible types
            return new PrimitiveVS<>(false).restrict(pc);
        }
        return tuple.symbolicEquals(cmp.tuple, pc);
    }

    @Override
    public Guard getUniverse() {
        return tuple.getUniverse();
    }
}
