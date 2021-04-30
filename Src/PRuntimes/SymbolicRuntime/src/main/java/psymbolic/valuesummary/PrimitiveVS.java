package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * Represents primitive value summary
 * @param <T> Type of value stored in the primitive value summary
 */
public class PrimitiveVS<T> implements ValueSummary<PrimitiveVS<T>> {
    private final List<GuardedValue<T>> guardedValuesList;
    /** Cached set of values */
    private Set<T> values;
    /** Cached universe */
    private Guard universe;

    /** The guards on these values *must* be mutually exclusive.
     *
     * In other words, for any two 'value1', 'value2' of type T, the following must be identically false:
     *
     *      and(guardedValues.get(value1), guardedValues.get(value2))
     *
     *  The map 'guardedValues' should never be modified.
     */
    private final Map<T, Guard> guardedValues;

    /** Make a new PrimVS with the largest possible universe containing only the specified value
     *
     * @param value The value that the PrimVS contains
     */
    public PrimitiveVS(T value) {
        this.guardedValues = Collections.singletonMap(value, Bdd.constTrue());
    }

    /** Caution: The caller must take care to ensure that the guards on the provided values are mutually exclusive.
     *
     * Additionally, the provided map should not be mutated after the object is constructed.
     */
    public PrimitiveVS(Map<T, Guard> guardedValues) {
        this.guardedValues = new HashMap<>();
        for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
            if (!entry.getValue().isFalse()) {
                this.guardedValues.put(entry.getKey(), entry.getValue());
            }
        }
    }

    /** Copy constructor for PrimVS
     *
     * @param old The PrimVS to copy
     */
    public PrimitiveVS(PrimitiveVS old) {
        this.guardedValues = new HashMap<>(old.guardedValues);
    }

    /** Make an empty PrimVS */
    public PrimitiveVS() { this(new HashMap<>()); }

    /** Get all the different possible guarded values */
    public List<GuardedValue<T>> getGuardedValues() {
        if (guardedValuesList == null)
            guardedValuesList = guardedValues.entrySet().stream()
                    .map(x -> new GuardedValue<T>(x.getKey(), x.getValue())).collect(Collectors.toList());
        return guardedValuesList;
    }

    /** Get all the different possible values */
    public Set<T> getValues() {
        if (values == null)
            values = guardedValues.keySet();
        return values;
    }

    @Override
    public Guard getUniverse() {
        if (universe == null)
            universe = Guard.orMany(new ArrayList<>(guardedValues.values()));
        return universe;
    }

    /** Get whether or not the provided value is a possibility
     *
     * @param value The provided value
     * @return Whether or not the provided value is a possibility
     */
    public boolean hasValue(T value) {
        return guardedValues.containsKey(value);
    }

    /** Get the guard for a given value
     *
     * @param value The value for which the guard should be gotten
     * @return The guard for the provided value
     */
    public Guard getGuard(T value) {
        return guardedValues.getOrDefault(value, Guard.constFalse());
    }

    public <U> PrimitiveVS<U> apply(Function<T, U> function) {
        final Map<U, Guard> results = new HashMap<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final U mapped = function.apply(guardedValue.getValue());
            results.merge(mapped, guardedValue.getGuard(), Guard::or);
        }

        return new PrimitiveVS<T>(results);
    }

    /** Remove the provided PrimVS values from the set of values
     *
     * @param rm The PrimVS to remove
     * @return The PrimVS after removal
     */
    public PrimitiveVS<T> remove(PrimitiveVS<T> rm) {
        Bdd guardToRemove = Bdd.constFalse();
        for (GuardedValue<T> guardedValue : rm.getGuardedValues()) {
            guardToRemove = guardToRemove.or(this.guard(guardedValue.guard).getGuard(guardedValue.value));
        }
        return this.guard(guardToRemove.not());
    }

    public <U, V> PrimitiveVS<V>
    apply2(PrimitiveVS<U> summary2, BiFunction<T, U, V> function) {
        final Map<V, Bdd> results = new HashMap<>();

        for (GuardedValue<T> val1 : this.getGuardedValues()) {
            for (GuardedValue<U> val2: summary2.getGuardedValues()) {
                final Bdd combinedGuard = val1.guard.and(val2.guard);
                if (combinedGuard.isConstFalse()) {
                    continue;
                }

                final V mapped = function.apply(val1.value, val2.value);
                results.merge(mapped, combinedGuard, Bdd::or);
            }
        }

        return new PrimitiveVS<>(results);
    }


    public <Target extends ValueSummary<Target>>
    Target applyVS(
        Target mergeWith,
        Function<T, Target> function
    ) {
        final List<Target> toMerge = new ArrayList<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final Target mapped = function.apply(guardedValue.getValue());
            toMerge.add(mapped.guard(guardedValue.getGuard()));
        }

        return mergeWith.merge(toMerge);
    }

    @Override
    public boolean isEmptyVS() {
        return guardedValues.isEmpty();
    }

    @Override
    public PrimitiveVS<T> guard(Bdd guard) {
        final Map<T, Bdd> result = new HashMap<>();

        for (Map.Entry<T, Bdd> entry : guardedValues.entrySet()) {
            final Bdd newEntryGuard = entry.getValue().and(guard);
            if (!newEntryGuard.isConstFalse()) {
                result.put(entry.getKey(), newEntryGuard);
            }
        }

        return new PrimitiveVS<>(result);
    }

    @Override
    public PrimitiveVS<T> update(Bdd guard, PrimitiveVS<T> update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }


    @Override
    public PrimitiveVS<T> merge(Iterable<PrimitiveVS<T>> summaries) {
        final Map<T, Bdd> result = new HashMap<>(guardedValues);

        for (PrimitiveVS<T> summary : summaries) {
            for (Map.Entry<T, Bdd> entry : summary.guardedValues.entrySet()) {
                result.merge(entry.getKey(), entry.getValue(), Bdd::or);
            }
        }

        PrimitiveVS<T> res = new PrimitiveVS<>(result);
        return res;
    }

    @Override
    public PrimitiveVS<T> merge(PrimitiveVS<T> summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(PrimitiveVS<T> cmp, Bdd pc) {
        Bdd equalCond = Bdd.constFalse();
        for (Map.Entry<T, Bdd> entry : this.guardedValues.entrySet()) {
            if (cmp.guardedValues.containsKey(entry.getKey())) {
                equalCond = equalCond.or(entry.getValue().and(cmp.guardedValues.get(entry.getKey())));
            }
        }
        equalCond = equalCond.or(getUniverse().and(cmp.getUniverse()).not());
        return BoolUtils.fromTrueGuard(pc.and(equalCond));
    }

    @Override
    public String toString() {
        return getValues().toString();
    }

}
