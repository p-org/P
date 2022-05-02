package psymbolic.valuesummary;

import java.util.*;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * Represents a primitive value summary (Boolean, Integer, Float, String)
 * @param <T> Type of value stored in the primitive value summary
 */
public class PrimitiveVS<T> implements ValueSummary<PrimitiveVS<T>> {
    /**
     * A primitive value is a collection of guarded values
     *
     * The guards on these values *must* be mutually exclusive.
     * In other words, for any two 'value1', 'value2' of type T, the following must be identically false:
     *
     *      and(guardedValues.get(value1), guardedValues.get(value2))
     *
     *  The map 'guardedValues' should never be modified.
     */
    private final Map<T, Guard> guardedValues;

    /** Cached list of guarded values */
    private List<GuardedValue<T>> guardedValuesList;
    /** Cached set of values */
    private Set<T> values = null;

    /** Cached universe */
    private Guard universe = null;

    /** Get all the different possible guarded values */
    public List<GuardedValue<T>> getGuardedValues() {
        if (guardedValuesList == null)
            guardedValuesList = guardedValues.entrySet().stream()
                    .map(x -> new GuardedValue<T>(x.getKey(), x.getValue())).collect(Collectors.toList());
        return guardedValuesList;
    }

    @Override
    public Guard getUniverse() {
        if(universe == null)
            universe = Guard.orMany(new ArrayList<>(guardedValues.values()));
        return universe;
    }

    public Set<T> getValues() {
        if(values == null)
            values = new HashSet(guardedValues.keySet());
        return values;
    }

    /**
     * Create a PrimitiveVS with the largest possible universe (restrict = true) containing only the specified value
     *
     * @param value A primitive value summary containing the passed value under the `true` restrict
     */
    public PrimitiveVS(T value) {
        this.guardedValues = Collections.singletonMap(value, Guard.constTrue());
    }

    /**
     * Create a value summary with the given guarded values
     * Caution: The caller must take care to ensure that the guards on the provided values are mutually exclusive.
     */
    public PrimitiveVS(Map<T, Guard> guardedValues) {
        this.guardedValues = new HashMap<>();
        for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
            if (!entry.getValue().isFalse()) {
                this.guardedValues.put(entry.getKey(), entry.getValue());
            }
        }
    }

    /** Copy constructor for PrimitiveVS
     *
     * @param old The PrimitiveVS to copy
     */
    public PrimitiveVS(PrimitiveVS<T> old) {
        this(old.guardedValues);
    }

    /** Make an empty PrimVS */
    public PrimitiveVS() { this(new HashMap<>()); }

    /**
     * Copy the value summary
     *
     * @return A new cloned copy of the value summary
     */
    public PrimitiveVS<T> getCopy() {
        return new PrimitiveVS(this);
    }

    /** Check if the provided value is a possibility
     *
     * @param value The provided value
     * @return Whether or not the provided value is a possibility
     */
    public boolean hasValue(T value) {
        return guardedValues.containsKey(value);
    }

    /**
     * Get the restrict for a given value
     *
     * @param value The value for which the restrict should be gotten
     * @return The restrict for the provided value (false if the value does not exist in the VS)
     */
    public Guard getGuardFor(T value) {
        return guardedValues.getOrDefault(value, Guard.constFalse());
    }

    /**
     * Apply the function `func` to each guarded value of type T in the Value Summary and return a primitive value summary with values of type U
     * @param func Function to be applied
     * @param <U> Type of the values in the resultant primitive value summary
     * @return A primitive value summary with values of type U
     */
    public <U> PrimitiveVS<U> apply(Function<T, U> func) {
        final Map<U, Guard> results = new HashMap<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final U mapped = func.apply(guardedValue.getValue());
            results.merge(mapped, guardedValue.getGuard(), Guard::or);
        }

        return new PrimitiveVS<U>(results);
    }

    /**
     * Remove the provided Primitive VS values from the set of values
     *
     * @param rm The PrimitiveVS values to remove from the current value summary
     * @return The PrimitiveVS after removal of values
     */
    @Deprecated
    public PrimitiveVS<T> remove(PrimitiveVS<T> rm) {
        Guard guardToRemove = Guard.constFalse();
        for (GuardedValue<T> guardedValue : rm.getGuardedValues()) {
            guardToRemove = guardToRemove.or(this.restrict(guardedValue.getGuard()).getGuardFor(guardedValue.getValue()));
        }
        return this.restrict(guardToRemove.not());
    }

    public <U, V> PrimitiveVS<V>
    apply(PrimitiveVS<U> summary2, BiFunction<T, U, V> function) {
        final Map<V, Guard> results = new HashMap<>();

        for (GuardedValue<T> val1 : this.getGuardedValues()) {
            for (GuardedValue<U> val2: summary2.getGuardedValues()) {
                final Guard combinedGuard = val1.getGuard().and(val2.getGuard());
                if (combinedGuard.isFalse()) {
                    continue;
                }
                final V mapped = function.apply(val1.getValue(), val2.getValue());
                results.merge(mapped, combinedGuard, Guard::or);
            }
        }

        return new PrimitiveVS<>(results);
    }


    public <Target> PrimitiveVS<Target> apply(
        PrimitiveVS<Target> mergeWith,
        Function<T, Target> function
    ) {
        final List<PrimitiveVS<Target>> toMerge = new ArrayList<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final Target mapped = function.apply(guardedValue.getValue());
            toMerge.add(new PrimitiveVS<>(mapped).restrict(guardedValue.getGuard()));
        }

        return mergeWith.merge(toMerge);
    }

    @Override
    public boolean isEmptyVS() {
        return guardedValues.isEmpty();
    }

    @Override
    public PrimitiveVS<T> restrict(Guard guard) {
        if(guard.equals(getUniverse()))
            return new PrimitiveVS<>(this);

        final Map<T, Guard> result = new HashMap<>();

        for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
            final Guard newEntryGuard = entry.getValue().and(guard);
            if (!newEntryGuard.isFalse()) {
                result.put(entry.getKey(), newEntryGuard);
            }
        }
        return new PrimitiveVS<>(result);
    }

    @Override
    public PrimitiveVS<T> updateUnderGuard(Guard guard, PrimitiveVS<T> updateVal) {
        return this.restrict(guard.not()).merge(Collections.singletonList(updateVal.restrict(guard)));
    }


    @Override
    public PrimitiveVS<T> merge(Iterable<PrimitiveVS<T>> summaries) {
        final Map<T, Guard> result = new HashMap<>(guardedValues);

        for (PrimitiveVS<T> summary : summaries) {
            for (Map.Entry<T, Guard> entry : summary.guardedValues.entrySet()) {
                result.merge(entry.getKey(), entry.getValue(), Guard::or);
            }
        }

        return new PrimitiveVS<>(result);
    }

    @Override
    public PrimitiveVS<T> merge(PrimitiveVS<T> summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(PrimitiveVS<T> cmp, Guard pc) {
        Guard equalCond = Guard.constFalse();
        for (Map.Entry<T, Guard> entry : this.guardedValues.entrySet()) {
            if (cmp.guardedValues.containsKey(entry.getKey())) {
                equalCond = equalCond.or(entry.getValue().and(cmp.guardedValues.get(entry.getKey())));
            }
        }
        equalCond = equalCond.or(getUniverse().and(cmp.getUniverse()).not());
        return BooleanVS.trueUnderGuard(pc.and(equalCond));
    }

    @Override
    public String toString() {
        return getValues().toString();
    }

    public String toStringDetailed() {
        String str = "[";
        for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
            str += entry.getKey().toString() + " @ " + entry.getValue().toString() + ", ";
        }
        str += "]";
        return str;
    }

}
