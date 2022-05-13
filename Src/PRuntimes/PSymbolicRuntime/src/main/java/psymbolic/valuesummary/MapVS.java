package psymbolic.valuesummary;

import psymbolic.valuesummary.util.ValueSummaryChecks;
import java.util.*;

/** Class for map value summaries */
public class MapVS<K, V extends ValueSummary<V>> implements ValueSummary<MapVS<K,V>> {
    /** The set of keys */
    public final SetVS<PrimitiveVS<K>> keys;
    /** The mapping from all possible keys to values */
    public final Map<K, V> entries;

    /** Make a new MapVS with the specified set of keys and mapping
     *
     * @param keys The set of keys
     * @param entries The mapping from all possible keys to value summaries
     */
    public MapVS(SetVS<PrimitiveVS<K>> keys, Map<K, V> entries) {
        this.keys = keys;
        this.entries = entries;
    }

    /** Make a new MapVS with the specified universe
     *
     * @param universe The universe for the new MapVS
     */
    public MapVS(Guard universe) {
        this.keys = new SetVS<>(universe);
        this.entries = new HashMap<>();
    }

    /** Copy-constructor for MapVS
     * @param old The MapVS to copy
     */
    public MapVS(MapVS<K, V> old) {
        this(new SetVS<>(old.keys), new HashMap<>(old.entries));
    }

    /**
     * Copy the value summary
     *
     * @return A new cloned copy of the value summary
     */
    public MapVS<K, V> getCopy() {
        return new MapVS(this);
    }

    /** Get the number of entries in the MapVS
     *
     * @return The size of the MapVS
     * */
    public PrimitiveVS<Integer> getSize() {
        return keys.size();
    }

    /** Get the keys in the MapVS as a ListVS
     *
     * @return The keys of the MapVS in a ListVS
     */
    public ListVS<PrimitiveVS<K>> getKeys() {
        return this.keys.getElements();
    }

    @Override
    public boolean isEmptyVS() {
        return keys.isEmptyVS();
    }

    @Override
    public MapVS<K, V> restrict(Guard guard) {
        final SetVS<PrimitiveVS<K>> newKeys = keys.restrict(guard);
        final Map<K, V> newEntries = new HashMap<>();

        for (Map.Entry<K, V> entry : entries.entrySet()) {
            final V newValue = entry.getValue().restrict(guard);
            if (!newValue.isEmptyVS()) {
                newEntries.put(entry.getKey(), newValue);
            }
        }
        return new MapVS<>(newKeys, newEntries);
    }

    @Override
    public MapVS<K, V> merge(Iterable<MapVS<K, V>> summaries) {
        final List<SetVS<PrimitiveVS<K>>> keysToMerge = new ArrayList<>();
        final Map<K, List<V>> valuesToMerge = new HashMap<>();

        // add this set of entries' values, too
        for (Map.Entry<K, V> entry : entries.entrySet()) {
            valuesToMerge
                    .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
                    .add(entry.getValue());
        }

        for (MapVS<K, V> summary : summaries) {
            keysToMerge.add(summary.keys);

            for (Map.Entry<K, V> entry : summary.entries.entrySet()) {
                valuesToMerge
                        .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
                        .add(entry.getValue());
            }
        }

        final SetVS<PrimitiveVS<K>> mergedKeys = keys.merge(keysToMerge);

        final Map<K, V> mergedValues = new HashMap<>();
        for (Map.Entry<K, List<V>> entriesToMerge : valuesToMerge.entrySet()) {
            List<V> toMerge = entriesToMerge.getValue();
            if (toMerge.size() > 0) {
                mergedValues.put(entriesToMerge.getKey(), toMerge.get(0).merge(toMerge.subList(1, toMerge.size())));
            }
        }

        return new MapVS<>(mergedKeys, mergedValues);
    }

    @Override
    public MapVS<K, V> merge(MapVS<K, V> summary) {
        return merge(Collections.singletonList(summary));
    }

    @Override
    public MapVS<K, V> updateUnderGuard(Guard guard, MapVS<K, V> update) {
        return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(MapVS<K, V> cmp, Guard pc) {
        Guard equalCond = Guard.constFalse();
        Guard guard = BooleanVS.getTrueGuard(this.keys.symbolicEquals(cmp.keys, Guard.constTrue()));
        ListVS<PrimitiveVS<K>> thisSet = this.restrict(guard).keys.getElements();
        ListVS<PrimitiveVS<K>> cmpSet = cmp.restrict(guard).keys.getElements();

        if (thisSet.isEmpty() && cmpSet.isEmpty()) return BooleanVS.trueUnderGuard(pc.and(guard));

        while (!thisSet.isEmpty()) {
            PrimitiveVS<K> thisVal = thisSet.get(new PrimitiveVS<>(0).restrict(guard));
            PrimitiveVS<K> cmpVal = cmpSet.get(new PrimitiveVS<>(0).restrict(guard));
            assert(ValueSummaryChecks.equalUnder(thisVal, cmpVal, guard));
            for (GuardedValue<K> key : thisVal.getGuardedValues()) {
                PrimitiveVS<Boolean> compareVals = entries.get(key.getValue()).restrict(key.getGuard())
                        .symbolicEquals(cmp.entries.get(key.getValue()).restrict(key.getGuard()), guard);
                equalCond = equalCond.or(BooleanVS.getTrueGuard(compareVals));
            }
            thisSet = thisSet.removeAt(new PrimitiveVS<>(0).restrict(thisVal.getUniverse()));
            cmpSet = cmpSet.removeAt(new PrimitiveVS<>(0).restrict(thisVal.getUniverse()));
        }

        return BooleanVS.trueUnderGuard(pc.and(equalCond));
    }

    @Override
    public Guard getUniverse() {
        return keys.getUniverse();
    }

    /** Put a key-value pair into the MapVS
     *
     * @param keySummary The key VS
     * @param valSummary The value VS
     * @return The updated MapVS
     */
    public MapVS<K, V> put(PrimitiveVS<K> keySummary, V valSummary) {
        final SetVS<PrimitiveVS<K>> newKeys = keys.add(keySummary);
        final Map<K, V> newEntries = new HashMap<>(entries);
        for (GuardedValue<K> guardedKey : keySummary.getGuardedValues()) {
            V oldVal = entries.get(guardedKey.getValue());
            if (oldVal == null) {
                newEntries.put(guardedKey.getValue(), valSummary);
            } else {
                newEntries.put(guardedKey.getValue(), oldVal.updateUnderGuard(guardedKey.getGuard(), valSummary));
            }
        }

        return new MapVS<>(newKeys, newEntries);
    }

    /** Add a key-value pair into the MapVS
     *
     * @param keySummary The key value summary
     * @param valSummary The value value summary
     * @return The updated MapVS
     */
    public MapVS<K, V> add(PrimitiveVS<K> keySummary, V valSummary) {
        assert(ValueSummaryChecks.hasSameUniverse(keySummary.getUniverse(), valSummary.getUniverse()));
        return put(keySummary, valSummary);
    }

    /** Remove a key-value pair from the MapVS
     *
     * @param keySummary The key value summary
     * @return The updated MapVS
     */
    public MapVS<K, V> remove(PrimitiveVS<K> keySummary) {
        final SetVS<PrimitiveVS<K>> newKeys = keys.remove(keySummary);

        final Map<K, V> newEntries = new HashMap<>(entries);
        for (GuardedValue<K> guardedKey : keySummary.getGuardedValues()) {
            V oldVal = entries.get(guardedKey.getValue());
            if (oldVal == null) {
                continue;
            }

            final V remainingVal = oldVal.restrict(guardedKey.getGuard().not());
            if (remainingVal.isEmptyVS()) {
                newEntries.remove(guardedKey.getValue());
            } else {
                newEntries.put(guardedKey.getValue(), remainingVal);
            }
        }

        return new MapVS<>(newKeys, newEntries);
    }

    /** Get a value from from the MapVS
     *
     * @param keySummary The key value summary.
     * @return The option containing value corresponding to the key or an empty option if no such value
     */
    public V get(PrimitiveVS<K> keySummary) {
        if (!containsKey(keySummary).restrict(keySummary.getUniverse()).getGuardFor(false).isFalse()) {
            // there is a possibility that the key is not present
            throw new NoSuchElementException();
        }

        V merger = null;
        List<V> toMerge = new ArrayList<>();
        for (GuardedValue<K> key : keySummary.getGuardedValues()) {
            if (merger == null)
                merger = entries.get(key.getValue()).restrict(key.getGuard());
            toMerge.add(entries.get(key.getValue()).restrict(key.getGuard()));
        }

        assert merger != null;
        return merger.merge(toMerge);
    }

    /** Get whether the MapVS contains a
     *
     * @param keySummary The key ValueSummary
     * @return Whether or not the MapVS contains a key
     */
    public PrimitiveVS<Boolean> containsKey(PrimitiveVS<K> keySummary) {
        return keys.contains(keySummary);
    }

}
