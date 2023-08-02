package psym.valuesummary;

import java.util.*;
import lombok.Getter;
import psym.runtime.machine.Machine;
import psym.utils.Assert;

/** Class for map value summaries */
public class MapVS<K, T extends ValueSummary<T>, V extends ValueSummary<V>>
    implements ValueSummary<MapVS<K, T, V>> {
  /** The set of keys */
  public final SetVS<T> keys;
  /** The mapping from all possible keys to values */
  public final Map<K, V> entries;
  /** The mapping from all possible keys to values */
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;

  /**
   * Make a new MapVS with the specified set of keys and mapping
   *
   * @param keys The set of keys
   * @param entries The mapping from all possible keys to value summaries
   */
  public MapVS(SetVS<T> keys, Map<K, V> entries) {
    this.keys = keys;
    this.entries = entries;
    this.concreteHash = computeConcreteHash();
  }

  /**
   * Make a new MapVS with the specified universe
   *
   * @param universe The universe for the new MapVS
   */
  public MapVS(Guard universe) {
    this.keys = new SetVS<>(universe);
    this.entries = new HashMap<>();
    this.concreteHash = computeConcreteHash();
  }

  /**
   * Copy-constructor for MapVS
   *
   * @param old The MapVS to copy
   */
  public MapVS(MapVS<K, T, V> old) {
    this(new SetVS<>(old.keys), new HashMap<>(old.entries));
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public MapVS<K, T, V> getCopy() {
    return new MapVS(this);
  }

  /**
   * Permute the value summary
   *
   * @param m1 first machine
   * @param m2 second machine
   * @return A new cloned copy of the value summary with m1 and m2 swapped
   */
  public MapVS<K, T, V> swap(Machine m1, Machine m2) {
    Map<K, V> newEntries = new HashMap<>();
    for (Map.Entry<K, V> kv : this.entries.entrySet()) {
      K key = kv.getKey();
      if (key instanceof Machine) {
        Machine machineKey = (Machine) key;
        if (key.equals(m1)) {
          key = (K) m2;
        } else if (key.equals(m2)) {
          key = (K) m1;
        }
      }
      V val = kv.getValue().swap(m1, m2);
      newEntries.put(key, val);
    }
    return new MapVS(this.keys.swap(m1, m2), newEntries);
  }

  /**
   * Get the number of entries in the MapVS
   *
   * @return The size of the MapVS
   */
  public PrimitiveVS<Integer> size() {
    return keys.size();
  }

  /**
   * Get the keys in the MapVS as a ListVS
   *
   * @return The keys of the MapVS in a ListVS
   */
  public ListVS<T> getKeys() {
    return this.keys.getElements();
  }

  /**
   * Get the values in the MapVS as a ListVS
   *
   * @return The values of the MapVS in a ListVS
   */
  public ListVS<V> getValues() {
    ListVS<V> result = new ListVS<V>(getUniverse());
    for (V value : this.entries.values()) {
      result = result.add(value);
    }
    return result;
  }

  @Override
  public boolean isEmptyVS() {
    return keys.isEmptyVS();
  }

  @Override
  public MapVS<K, T, V> restrict(Guard guard) {
    final SetVS<T> newKeys = keys.restrict(guard);
    final Map<K, V> newEntries = new HashMap<>();

    if (!newKeys.isEmptyVS()) {
      for (Map.Entry<K, V> entry : entries.entrySet()) {
        final V newValue = entry.getValue().restrict(guard);
        newEntries.put(entry.getKey(), newValue);
      }
    }
    return new MapVS<>(newKeys, newEntries);
  }

  @Override
  public MapVS<K, T, V> merge(Iterable<MapVS<K, T, V>> summaries) {
    final List<SetVS<T>> keysToMerge = new ArrayList<>();
    final Map<K, List<V>> valuesToMerge = new HashMap<>();

    // add this set of entries' values, too
    for (Map.Entry<K, V> entry : entries.entrySet()) {
      valuesToMerge
          .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
          .add(entry.getValue());
    }

    for (MapVS<K, T, V> summary : summaries) {
      keysToMerge.add(summary.keys);

      for (Map.Entry<K, V> entry : summary.entries.entrySet()) {
        valuesToMerge
            .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
            .add(entry.getValue());
      }
    }

    final SetVS<T> mergedKeys = keys.merge(keysToMerge);

    final Map<K, V> mergedValues = new HashMap<>();
    for (Map.Entry<K, List<V>> entriesToMerge : valuesToMerge.entrySet()) {
      List<V> toMerge = entriesToMerge.getValue();
      if (toMerge.size() > 0) {
        mergedValues.put(
            entriesToMerge.getKey(), toMerge.get(0).merge(toMerge.subList(1, toMerge.size())));
      }
    }

    return new MapVS<>(mergedKeys, mergedValues);
  }

  @Override
  public MapVS<K, T, V> merge(MapVS<K, T, V> summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public MapVS<K, T, V> updateUnderGuard(Guard guard, MapVS<K, T, V> update) {
    return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(MapVS<K, T, V> cmp, Guard pc) {
    if (cmp == null) {
      return BooleanVS.trueUnderGuard(Guard.constFalse());
    }

    Guard guard = BooleanVS.getTrueGuard(this.keys.symbolicEquals(cmp.keys, pc));
    ListVS<T> thisSet = this.restrict(guard).getKeys();
    ListVS<T> cmpSet = cmp.restrict(guard).getKeys();

    if (thisSet.isEmpty() && cmpSet.isEmpty())
      return BooleanVS.trueUnderGuard(guard).restrict(getUniverse().and(cmp.getUniverse()));

    Guard equalCond = guard;
    while (!thisSet.isEmpty()) {
      T thisVal = thisSet.get(new PrimitiveVS<>(0, guard));
      for (GuardedValue<?> key : ValueSummary.getGuardedValues(thisVal)) {
        PrimitiveVS<Boolean> compareVals =
            entries
                .get(key.getValue())
                .restrict(key.getGuard())
                .symbolicEquals(
                    cmp.entries.get(key.getValue()).restrict(key.getGuard()), equalCond);
        equalCond = equalCond.and(BooleanVS.getTrueGuard(compareVals));
      }
      thisSet = thisSet.removeAt(new PrimitiveVS<>(0, thisVal.getUniverse()));
    }

    return BooleanVS.trueUnderGuard(equalCond).restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    return keys.getUniverse();
  }

  /**
   * Put a key-value pair into the MapVS
   *
   * @param keySummary The key VS
   * @param valSummary The value VS
   * @return The updated MapVS
   */
  public MapVS<K, T, V> put(T keySummary, V valSummary) {
    final SetVS<T> newKeys = keys.add(keySummary);
    final Map<K, V> newEntries = new HashMap<>(entries);
    for (GuardedValue<?> guardedKey : ValueSummary.getGuardedValues(keySummary)) {
      V oldVal = entries.get(guardedKey.getValue());
      if (oldVal == null) {
        newEntries.put((K) guardedKey.getValue(), valSummary);
      } else {
        newEntries.put(
            (K) guardedKey.getValue(), oldVal.updateUnderGuard(guardedKey.getGuard(), valSummary));
      }
    }

    return new MapVS<>(newKeys, newEntries);
  }

  /**
   * Add a key-value pair into the MapVS
   *
   * @param keySummary The key value summary
   * @param valSummary The value value summary
   * @return The updated MapVS
   */
  public MapVS<K, T, V> add(T keySummary, V valSummary) {
    V merger = null;
    List<V> toMerge = new ArrayList<>();
    for (GuardedValue<?> key :
        ValueSummary.getGuardedValues(
            keySummary.restrict(containsKey(keySummary).getGuardFor(true)))) {
      if (entries.containsKey(key.getValue())) {
        V val = entries.get(key.getValue());
        if (merger == null) merger = val.restrict(key.getGuard());
        toMerge.add(val.restrict(key.getGuard()));
      }
    }

    if (merger != null) {
      V oldVal = merger.merge(toMerge);
      Assert.prop(
          false,
          String.format(
              "ArgumentException: An item with the same key has already been added. Key: %s, Value: %s",
              keySummary, oldVal),
          keySummary.getUniverse().and(oldVal.getUniverse()));
    }
    return put(keySummary, valSummary);
  }

  /**
   * Remove a key-value pair from the MapVS
   *
   * @param keySummary The key value summary
   * @return The updated MapVS
   */
  public MapVS<K, T, V> remove(T keySummary) {
    final SetVS<T> newKeys = keys.remove(keySummary);

    final Map<K, V> newEntries = new HashMap<>(entries);
    for (GuardedValue<?> guardedKey : ValueSummary.getGuardedValues(keySummary)) {
      V oldVal = entries.get(guardedKey.getValue());
      if (oldVal == null) {
        continue;
      }

      final V remainingVal = oldVal.restrict(guardedKey.getGuard().not());
      if (remainingVal.isEmptyVS()) {
        newEntries.remove(guardedKey.getValue());
      } else {
        newEntries.put((K) guardedKey.getValue(), remainingVal);
      }
    }

    return new MapVS<>(newKeys, newEntries);
  }

  /**
   * Get a value from from the MapVS
   *
   * @param keySummary The key value summary.
   * @return The option containing value corresponding to the key or an empty option if no such
   *     value
   */
  public V get(T keySummary) {
    // there is a possibility that the key is not present
    if (keySummary.isEmptyVS()) {
      Assert.prop(
          false,
          String.format(
              "KeyNotFoundException: The given key %s was not present in the dictionary %s.",
              keySummary, this),
          Guard.constTrue());
    }
    if (!containsKey(keySummary).getGuardFor(false).isFalse()) {
      Assert.prop(
          false,
          String.format(
              "KeyNotFoundException: The given key %s was not present in the dictionary %s.",
              keySummary, this),
          containsKey(keySummary).getGuardFor(false));
    }

    V merger = null;
    List<V> toMerge = new ArrayList<>();
    for (GuardedValue<?> key : ValueSummary.getGuardedValues(keySummary)) {
      V val = entries.get(key.getValue());
      if (merger == null) merger = val.restrict(key.getGuard());
      toMerge.add(val.restrict(key.getGuard()));
    }

    assert merger != null;
    return merger.merge(toMerge);
  }

  /**
   * Get a value from the MapVS or return default value if key does not exist
   *
   * @param keySummary The key value summary.
   * @param defaultValue The default value.
   * @return The option containing value corresponding to the key or default option if no such value
   */
  public V getOrDefault(T keySummary, V defaultValue) {
    // there is a possibility that the key is not present
    if (keySummary.isEmptyVS()) {
      return defaultValue;
    }
    if (!containsKey(keySummary).getGuardFor(false).isFalse()) {
      return defaultValue.restrict(keySummary.getUniverse());
    }

    V merger = null;
    List<V> toMerge = new ArrayList<>();
    for (GuardedValue<?> key : ValueSummary.getGuardedValues(keySummary)) {
      V val = entries.getOrDefault(key.getValue(), defaultValue);
      if (merger == null) merger = val.restrict(key.getGuard());
      toMerge.add(val.restrict(key.getGuard()));
    }

    assert merger != null;
    return merger.merge(toMerge);
  }

  /**
   * Get whether the MapVS contains a
   *
   * @param keySummary The key ValueSummary
   * @return Whether or not the MapVS contains a key
   */
  public PrimitiveVS<Boolean> containsKey(T keySummary) {
    return keys.contains(keySummary);
  }

  @Override
  public int computeConcreteHash() {
    int hashCode = 1;
    for (Map.Entry<K, V> entry : entries.entrySet()) {
      hashCode = 31 * hashCode + (entry.getKey() == null ? 0 : entry.getKey().hashCode());
      hashCode =
          31 * hashCode + (entry.getValue() == null ? 0 : entry.getValue().getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public String toString() {
    return "Map[" + "  keys: " + keys + ",  values: " + new TreeMap<>(entries) + "]";
  }

  public String toStringDetailed() {
    StringBuilder out = new StringBuilder();
    out.append("Map[ keys: ");
    out.append(keys.toStringDetailed());
    out.append(", values: {");
    for (Map.Entry<K, V> entry : entries.entrySet()) {
      out.append(entry.getKey()).append(" -> ");
      out.append(entry.getValue().toStringDetailed()).append(", ");
    }
    out.append("} ]");
    return out.toString();
  }
}
