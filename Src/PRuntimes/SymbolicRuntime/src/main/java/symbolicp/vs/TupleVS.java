package symbolicp.vs;

import symbolicp.bdd.Bdd;

import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.IntStream;

/** Class for tuple value summaries */
public class TupleVS implements ValueSummary<TupleVS> {
    /** The fields of the tuple */
    private final ValueSummary[] fields;
    /** The classes of the fields of the tuple */
    private final Class[] classes;

    public TupleVS(TupleVS tuple) {
        this.fields = Arrays.copyOf(tuple.fields, tuple.fields.length);
        this.classes = Arrays.copyOf(tuple.classes, tuple.classes.length);
    }

    /** Make a new TupleVS from the provided items */
    public TupleVS(ValueSummary... items) {
        this.fields = items;
        this.classes = Arrays.asList(items).stream().map(x -> x.getClass())
                .collect(Collectors.toList()).toArray(new Class[items.length]);
    }

    /** Get the arity of the TupleVS
     * @return The arity of the TupleVS */
    public int getArity() {
        return fields.length;
    }

    /** Get the i-th value in the TupleVS
     * @param i The index to get from the TupleVS
     * @return The value at index i */
    public ValueSummary getField(int i) {
        return fields[i];
    }

    /** Set the i-th value in the tuTupleVSple to the provided value
     * @param i The index to set in the TupleVS
     * @param val The value to set in the TupleVS
     * @return The result after updating the TupleVS */
    public TupleVS setField(int i, ValueSummary val) {
        final ValueSummary[] newItems = new ValueSummary[fields.length];
        System.arraycopy(fields, 0, newItems, 0, fields.length);
        if (!(val.getClass().equals(classes[i]))) throw new ClassCastException();
        newItems[i] = val;
        return new TupleVS(newItems);
    }

    @Override
    public boolean isEmptyVS() {
        // Optimization: Tuples should always be nonempty,
        // and all fields should exist under the same conditions
        return fields[0].isEmptyVS();
    }

    @Override
    public TupleVS guard(Bdd guard) {
        ValueSummary[] resultFields = new ValueSummary[fields.length];
        for (int i = 0; i < fields.length; i++) {
            resultFields[i] = fields[i].guard(guard);
        }
        return new TupleVS(resultFields);
    }

    @Override
    public TupleVS merge(Iterable<TupleVS> summaries) {
        List<ValueSummary> resultList = Arrays.asList(fields);
        for (TupleVS summary : summaries) {
            for (int i = 0; i < summary.fields.length; i++) {
                if (i < resultList.size()) {
                    resultList.set(i, resultList.get(i).merge(summary.fields[i]));
                } else {
                    resultList.add(summary.fields[i]);
                }
            }
        }
        return new TupleVS(resultList.toArray(new ValueSummary[resultList.size()]));
    }

    @Override
    public TupleVS merge(TupleVS summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public TupleVS update(Bdd guard, TupleVS update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(TupleVS cmp, Bdd pc) {
        if (fields.length != cmp.fields.length) {
            return new PrimVS<>(false);
        }
        Bdd tupleEqual = IntStream.range(0, fields.length)
                .mapToObj((i) -> fields[i].symbolicEquals(cmp.fields[i], pc).getGuard(Boolean.TRUE))
                .reduce(Bdd::and)
                .orElse(Bdd.constTrue());
        return BoolUtils.fromTrueGuard(pc.and(tupleEqual));
    }

    @Override
    public Bdd getUniverse() {
        // Optimization: Tuples should always be nonempty,
        // and all fields should exist under the same conditions
        return fields[0].getUniverse();
    }

    @Override
    public String toString() {
        String str = "( ";
        for (int i = 0; i < classes.length; i++) {
            str += (classes[i]).cast(fields[i]).toString() + " ";
        }
        str += ")";
        return str;
    }
}
