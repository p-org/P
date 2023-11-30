package psym.valuesummary;

import java.util.*;
import java.util.function.BiFunction;
import java.util.function.Function;
import java.util.stream.Collectors;
import lombok.Getter;
import psym.runtime.machine.Machine;

/**
 * Represents a primitive value summary (Boolean, Integer, Float, String)
 *
 * @param <T> Type of value stored in the primitive value summary
 */
public class PrimitiveVS<T> implements ValueSummary<PrimitiveVS<T>> {
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;
  @Getter
  /** Concrete value used in explicit-state search */
  private final T concreteValue;

  /**
   * A primitive value is a collection of guarded values
   *
   * <p>The guards on these values *must* be mutually exclusive. In other words, for any two
   * 'value1', 'value2' of type T, the following must be identically false:
   *
   * <p>and(guardedValues.get(value1), guardedValues.get(value2))
   *
   * <p>The map 'guardedValues' should never be modified.
   */
  private final Map<T, Guard> guardedValues;

  /** Cached list of guarded values */
  private List<GuardedValue<T>> guardedValuesList;
  /** Cached set of values */
  private Set<T> values = null;

  /** Cached universe */
  private Guard universe = null;

  /**
   * Create a PrimitiveVS with a single guarded value
   *
   * @param value A primitive value summary containing the passed value under the guard restrict
   */
  public PrimitiveVS(T value, Guard guard) {
    this.guardedValues = Collections.singletonMap(value, guard);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Create a PrimitiveVS with the largest possible universe (restrict = true) containing only the
   * specified value
   *
   * @param value A primitive value summary containing the passed value under the `true` restrict
   */
  public PrimitiveVS(T value) {
    this(value, Guard.constTrue());
  }

  /**
   * Create a value summary with the given guarded values Caution: The caller must take care to
   * ensure that the guards on the provided values are mutually exclusive.
   */
  public PrimitiveVS(Map<T, Guard> guardedValues) {
    this.guardedValues = guardedValues;
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Create a value summary with the given guarded values Caution: The caller must take care to
   * ensure that the guards on the provided values are mutually exclusive.
   */
  public PrimitiveVS(Map<T, Guard> guardedValues, boolean cleanup) {
    assert (cleanup);
    this.guardedValues = new HashMap<>();
    for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
      if (!entry.getValue().isFalse()) {
        this.guardedValues.put(entry.getKey(), entry.getValue());
      }
    }
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Copy constructor for PrimitiveVS
   *
   * @param old The PrimitiveVS to copy
   */
  public PrimitiveVS(PrimitiveVS<T> old) {
    this(new HashMap<>(old.guardedValues));
  }

  /** Make an empty PrimVS */
  public PrimitiveVS() {
    this(new HashMap<>());
  }

  /** Get all the different possible guarded values */
  public List<GuardedValue<T>> getGuardedValues() {
    if (guardedValuesList == null)
      guardedValuesList =
          guardedValues.entrySet().stream()
              .map(x -> new GuardedValue<T>(x.getKey(), x.getValue()))
              .collect(Collectors.toList());
    return guardedValuesList;
  }

  @Override
  public Guard getUniverse() {
    if (universe == null) universe = Guard.orMany(new ArrayList<>(guardedValues.values()));
    return universe;
  }

  public Set<T> getValues() {
    if (values == null) values = new HashSet(guardedValues.keySet());
    return values;
  }

  public Class getValueClass() {
    for (T val : getValues()) {
      if (val instanceof Machine) {
        return Machine.class;
      } else {
        return val.getClass();
      }
    }
    return this.getClass();
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public PrimitiveVS<T> getCopy() {
    return new PrimitiveVS(this);
  }

  public PrimitiveVS<T> swap(Map<Machine, Machine> mapping) {
    boolean swapped = false;
    Map<T, Guard> newGuardedValues = new HashMap<>();
    for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
      T key = entry.getKey();
      if (key instanceof Machine) {
        Machine origMachine = (Machine) key;
        Machine newMachine = mapping.get(origMachine);
        if (newMachine != null) {
          key = (T) newMachine;
          swapped = true;
        }
      }
      newGuardedValues.put(key, entry.getValue());
    }
    if (swapped) {
      return new PrimitiveVS<>(newGuardedValues);
    } else {
      return this;
    }
  }

  /**
   * Permute the value summary
   *
   * @param m1 first machine
   * @param m2 second machine
   * @return A new cloned copy of the value summary with m1 and m2 swapped
   */
  public PrimitiveVS<T> swap(PrimitiveVS<Machine> m1, PrimitiveVS<Machine> m2) {
    PrimitiveVS<T> result = this;
    boolean isMachineType = false;

    for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
      T key = entry.getKey();
      if (key instanceof Machine) {
        isMachineType = true;
        break;
      }
    }

    if (isMachineType) {
      Guard swapGuard = getUniverse().and(m1.getUniverse().and(m2.getUniverse()));
      if (!swapGuard.isFalse()) {
        Guard equalsM1 = this.symbolicEquals((PrimitiveVS<T>) m1, swapGuard).getGuardFor(true);
        if (!equalsM1.isFalse()) {
          result = result.updateUnderGuard(equalsM1, (PrimitiveVS<T>) m2);
        }
      }
    }
    return result;
  }

  /**
   * Check if the provided value is a possibility
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
   * Apply the function `func` to each guarded value of type T in the Value Summary and return a
   * primitive value summary with values of type U
   *
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
      guardToRemove =
          guardToRemove.or(
              this.restrict(guardedValue.getGuard()).getGuardFor(guardedValue.getValue()));
    }
    return this.restrict(guardToRemove.not());
  }

  public <U, V> PrimitiveVS<V> apply(PrimitiveVS<U> summary2, BiFunction<T, U, V> function) {
    final Map<V, Guard> results = new HashMap<>();

    for (GuardedValue<T> val1 : this.getGuardedValues()) {
      for (GuardedValue<U> val2 : summary2.getGuardedValues()) {
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
      PrimitiveVS<Target> mergeWith, Function<T, Target> function) {
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
    if (guard.equals(getUniverse())) return new PrimitiveVS<>(this);

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
    final Map<T, Guard> result = new HashMap<>();

    Guard nullUniverse = Guard.constFalse();
    Guard coveredUniverse = Guard.constFalse();
    Guard totalUniverse = getUniverse();
    for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
      if (entry.getKey() == null) {
        nullUniverse = nullUniverse.or(entry.getValue());
        continue;
      }
      coveredUniverse = coveredUniverse.or(entry.getValue());
      result.merge(entry.getKey(), entry.getValue(), Guard::or);
    }

    for (PrimitiveVS<T> summary : summaries) {
      totalUniverse = totalUniverse.or(summary.getUniverse());
      for (Map.Entry<T, Guard> entry : summary.guardedValues.entrySet()) {
        if (entry.getKey() == null) {
          nullUniverse = nullUniverse.or(entry.getValue());
          continue;
        }
        coveredUniverse = coveredUniverse.or(entry.getValue());
        result.merge(entry.getKey(), entry.getValue(), Guard::or);
      }
    }
    Guard remainingUniverse = totalUniverse.and(coveredUniverse.not());
    if (!remainingUniverse.isFalse()) {
      assert (remainingUniverse.implies(nullUniverse).isTrue());
      result.put(null, remainingUniverse);
    }

    return new PrimitiveVS<>(result);
  }

  @Override
  public PrimitiveVS<T> merge(PrimitiveVS<T> summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(PrimitiveVS<T> cmp_orig, Guard pc) {
    PrimitiveVS<T> cmp;
    boolean isNullCompare = false;
    if (cmp_orig == null) {
      isNullCompare = true;
      cmp = new PrimitiveVS<>((T) null);
    } else {
      cmp = cmp_orig;
    }
    Guard equalCond = Guard.constFalse();
    for (Map.Entry<T, Guard> entry : this.guardedValues.entrySet()) {
      if (isNullCompare) {
        if (entry.getKey() == null) {
          equalCond = equalCond.or(entry.getValue());
        }
      } else {
        if (cmp.guardedValues.containsKey(entry.getKey())) {
          equalCond = equalCond.or(entry.getValue().and(cmp.guardedValues.get(entry.getKey())));
        }
      }
    }
    equalCond = equalCond.or(getUniverse().and(cmp.getUniverse()).not());
    return BooleanVS.trueUnderGuard(pc.and(equalCond))
        .restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public int computeConcreteHash() {
    if (!guardedValues.isEmpty()) {
      T key = guardedValues.entrySet().iterator().next().getKey();
      return (key == null ? 0 : key.hashCode());
    } else {
      return 0;
    }
  }

  @Override
  public T computeConcreteValue() {
    if (!guardedValues.isEmpty()) {
      return guardedValues.entrySet().iterator().next().getKey();
    } else {
      return null;
    }
  }

  @Override
  public String toString() {
    StringBuilder out = new StringBuilder();
    Iterator itr = getValues().iterator();
    while (itr.hasNext()) {
      out.append(itr.next());
      if (itr.hasNext()) {
        out.append(", ");
      }
    }
    return out.toString();
  }

  public String toStringDetailed() {
    StringBuilder out = new StringBuilder();
    out.append("[");
    for (Map.Entry<T, Guard> entry : guardedValues.entrySet()) {
      out.append(entry.getKey()).append(" @ ");
      out.append(entry.getValue()).append(", ");
    }
    out.append("]");
    return out.toString();
  }
}
