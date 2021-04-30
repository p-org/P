package psymbolic.valuesummary;

import psymbolic.valuesummary.bdd.Bdd;

import java.util.*;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.stream.Collectors;

/** Class for primitive value summaries */
public class PrimVS<T> implements ValueSummary<PrimVS<T>> {
    private List<GuardedValue<T>> guardedValuesList;
    /** Cached set of values */
    private Set<T> values;
    /** Cached universe */
    private Bdd universe;

    /** The guards on these values *must* be mutually exclusive.
     *
     * In other words, for any two 'value1', 'value2' of type T, the following must be identically false:
     *
     *      and(guardedValues.get(value1), guardedValues.get(value2))
     *
     *  The map 'guardedValues' should never be modified.
     */
    private final Map<T, Bdd> guardedValues;

    /** Make a new PrimVS with the largest possible universe containing only the specified value
     *
     * @param value The value that the PrimVS contains
     */
    public PrimVS(T value) {
        this.guardedValues = Collections.singletonMap(value, Bdd.constTrue());
    }

    /** Caution: The caller must take care to ensure that the guards on the provided values are mutually exclusive.
     *
     * Additionally, the provided map should not be mutated after the object is constructed.
     */
    public PrimVS(Map<T, Bdd> guardedValues) {
        this.guardedValues = new HashMap<>();
        for (Map.Entry<T, Bdd> entry : guardedValues.entrySet()) {
            if (!entry.getValue().isConstFalse()) {
                this.guardedValues.put(entry.getKey(), entry.getValue());
            }
        }
    }

    /** Copy constructor for PrimVS
     *
     * @param old The PrimVS to copy
     */
    public PrimVS(PrimVS old) {
        this.guardedValues = new HashMap<>(old.guardedValues);
    }

    /** Make an empty PrimVS */
    public PrimVS() { this(new HashMap<>()); }

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
    public Bdd getUniverse() {
        if (universe == null)
            universe = Bdd.orMany(guardedValues.values().stream().collect(Collectors.toList()));
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
    public Bdd getGuard(T value) {
        return guardedValues.getOrDefault(value, Bdd.constFalse());
    }

    public <U> PrimVS<U> apply(Function<T, U> function) {
        final Map<U, Bdd> results = new HashMap<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final U mapped = function.apply(guardedValue.value);
            results.merge(mapped, guardedValue.guard, Bdd::or);
        }

        return new PrimVS<>(results);
    }

    /** Remove the provided PrimVS values from the set of values
     *
     * @param rm The PrimVS to remove
     * @return The PrimVS after removal
     */
    public PrimVS<T> remove(PrimVS<T> rm) {
        Bdd guardToRemove = Bdd.constFalse();
        for (GuardedValue<T> guardedValue : rm.getGuardedValues()) {
            guardToRemove = guardToRemove.or(this.guard(guardedValue.guard).getGuard(guardedValue.value));
        }
        return this.guard(guardToRemove.not());
    }

    public <U, V> PrimVS<V>
    apply2(PrimVS<U> summary2, BiFunction<T, U, V> function) {
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

        return new PrimVS<>(results);
    }


    public <Target extends ValueSummary<Target>>
    Target applyVS(
        Target mergeWith,
        Function<T, Target> function
    ) {
        final List<Target> toMerge = new ArrayList<>();

        for (GuardedValue<T> guardedValue : getGuardedValues()) {
            final Target mapped = function.apply(guardedValue.value);
            toMerge.add(mapped.guard(guardedValue.guard));
        }

        return mergeWith.merge(toMerge);
    }

    @Override
    public boolean isEmptyVS() {
        return guardedValues.isEmpty();
    }

    @Override
    public PrimVS<T> guard(Bdd guard) {
        final Map<T, Bdd> result = new HashMap<>();

        for (Map.Entry<T, Bdd> entry : guardedValues.entrySet()) {
            final Bdd newEntryGuard = entry.getValue().and(guard);
            if (!newEntryGuard.isConstFalse()) {
                result.put(entry.getKey(), newEntryGuard);
            }
        }

        return new PrimVS<>(result);
    }

    @Override
    public PrimVS<T> update(Bdd guard, PrimVS<T> update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }


    @Override
    public PrimVS<T> merge(Iterable<PrimVS<T>> summaries) {
        final Map<T, Bdd> result = new HashMap<>(guardedValues);

        for (PrimVS<T> summary : summaries) {
            for (Map.Entry<T, Bdd> entry : summary.guardedValues.entrySet()) {
                result.merge(entry.getKey(), entry.getValue(), Bdd::or);
            }
        }

        PrimVS<T> res = new PrimVS<>(result);
        return res;
    }

    @Override
    public PrimVS<T> merge(PrimVS<T> summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(PrimVS<T> cmp, Bdd pc) {
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
