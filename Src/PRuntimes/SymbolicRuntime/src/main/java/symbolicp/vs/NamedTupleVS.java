package symbolicp.vs;

import symbolicp.bdd.Bdd;

import java.util.*;

/** Class for named tuple value summaries */
public class NamedTupleVS<T> implements ValueSummary<NamedTupleVS<T>> {
    /** Mapping from names of the fields to their index in the underlying representation */
    private final Map<T, Integer> names;
    /** Underlying representation as a TupleVS */
    private final TupleVS tuple;

    private NamedTupleVS(Map<T, Integer> names, TupleVS tuple) {
        this.names = names;
        this.tuple = tuple;
    }

    public NamedTupleVS (NamedTupleVS<T> namedTuple) {
        this.names = new HashMap<>(namedTuple.names);
        this.tuple = new TupleVS(namedTuple.tuple);
    }

    /** Make a new NamedTupleVS with the provided names and fields
     * @param namesAndFields Alternating String and ValueSummary values where the Strings give the field names
     */
    public NamedTupleVS(Object... namesAndFields) {
        names = new HashMap<>();
        ValueSummary[] vs = new ValueSummary[namesAndFields.length/ 2];
        for (int i = 0; i < namesAndFields.length; i += 2) {
            T name = (T)namesAndFields[i];
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

    /** Set the value for a particular field
     * @param name The field name
     * @param val The value to set the specified field to
     * @return The result of updating the field
     */
    public NamedTupleVS<T> setField(String name, ValueSummary val) {
        return new NamedTupleVS(names, tuple.setField(names.get(name), val));
    }

    @Override
    public boolean isEmptyVS() {
        return tuple.isEmptyVS();
    }

    @Override
    public NamedTupleVS<T> guard(Bdd guard) {
        return new NamedTupleVS(names, tuple.guard(guard));
    }

    @Override
    public NamedTupleVS<T> merge(Iterable<NamedTupleVS<T>> summaries) {
        final List<TupleVS> tuples = new ArrayList<TupleVS>();

        for (NamedTupleVS<T> summary : summaries) {
            tuples.add(summary.tuple);
        }

        return new NamedTupleVS<>(names, tuple.merge(tuples));
    }

    @Override
    public NamedTupleVS<T> merge(NamedTupleVS<T> summaries) {
        return merge(Collections.singletonList(summaries));
    }

    @Override
    public NamedTupleVS<T> update(Bdd guard, NamedTupleVS<T> update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(NamedTupleVS<T> cmp, Bdd pc) {
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
