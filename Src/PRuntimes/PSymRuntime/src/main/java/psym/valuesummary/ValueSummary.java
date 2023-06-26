package psym.valuesummary;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

import lombok.Getter;
import psym.runtime.machine.Machine;
import psym.runtime.machine.events.Message;

public interface ValueSummary<T extends ValueSummary<T>> extends Serializable {

  static UnionVS castToAny(Guard pc, ValueSummary<?> toCast) {
    if (toCast instanceof UnionVS) {
      return (UnionVS) toCast.restrict(pc);
    }
    return new UnionVS(toCast).restrict(pc);
  }

  static ValueSummary<?> castToAnyCollection(Guard pc, ValueSummary<?> toCast) {
    if (toCast instanceof ListVS) {
      List<UnionVS> items = new ArrayList<>();
      ListVS toCastVs = (ListVS) toCast;
      for (Object item : toCastVs.getItems()) {
        items.add(ValueSummary.castToAny(pc, (ValueSummary<?>) item));
      }
      return new ListVS<>(toCastVs.size(), items);
    } else if (toCast instanceof SetVS) {
      SetVS toCastVs = (SetVS) toCast;
      ListVS elements = toCastVs.getElements();
      return new SetVS<>((ListVS) ValueSummary.castToAnyCollection(pc, elements));
    } else {
      throw new ClassCastException(
          String.format("Casting elements in %s to any is unsupported", toCast));
    }
  }

  /**
   * Casts an AnyVS to a ValueSummary type. If there is some non constantly false path constraint
   * under which the current pc is defined but not the guard corresponding to the specified type,
   * the function throws a ClassCastException. If the ValueSummary type is also a UnionVS, returns
   * the provided UnionVS.
   *
   * @param pc The path constraint guard to cast under
   * @param def The default value of the ValueSummary type to cast to
   * @param anyVal The UnionVS to cast from
   * @return A ValueSummary that can be casted into the provided type
   */
  static ValueSummary<?> castFromAny(Guard pc, ValueSummary<?> def, UnionVS anyVal) {
    ValueSummary<?> result;
    if (def instanceof UnionVS) {
      return anyVal;
    }
    if (anyVal == null) {
      return def.restrict(pc);
    }
    if (anyVal.isEmptyVS()) {
      return def.getCopy();
    }

    UnionVStype type;
    if (def instanceof NamedTupleVS) {
      type = UnionVStype.getUnionVStype(def.getClass(), ((NamedTupleVS) def).getNames());
    } else if (def instanceof TupleVS) {
      type = UnionVStype.getUnionVStype(def.getClass(), ((TupleVS) def).getNames());
    } else {
      type = UnionVStype.getUnionVStype(def.getClass(), null);
    }
    Guard typeGuard = anyVal.getGuardFor(type);
    Guard pcNotDefined = pc.and(typeGuard.not());
    Guard pcDefined = pc.and(typeGuard);
    if (pcDefined.isFalse()) {
      if (type.equals(UnionVStype.getUnionVStype(PrimitiveVS.class, null))) {
        return new PrimitiveVS<>(pc);
      }
      System.out.println(anyVal.restrict(typeGuard));
      throw new ClassCastException(
          String.format(
              "Casting to %s under path constraint %s is not defined for %s",
              type, pcNotDefined, anyVal));
    }
    result = anyVal.getValue(type).restrict(pc);
    return result;
  }

  static ValueSummary<?> castFromAnyCollection(
      Guard pc, ValueSummary<?> def, ValueSummary<?> anyVal) {
    if (anyVal instanceof ListVS) {
      List<ValueSummary> items = new ArrayList<>();
      ListVS toCastVs = (ListVS) anyVal;
      for (Object item : toCastVs.getItems()) {
        items.add(ValueSummary.castFromAny(pc, def, (UnionVS) item));
      }
      return new ListVS<>(toCastVs.size(), items);
    } else if (def instanceof SetVS) {
      SetVS toCastVs = (SetVS) anyVal;
      ListVS elements = toCastVs.getElements();
      return new SetVS<>((ListVS) ValueSummary.castFromAnyCollection(pc, def, elements));
    } else {
      throw new ClassCastException(
          String.format("Casting elements in %s to %s is unsupported", anyVal, def));
    }
  }

  /**
   * Get all the different possible guarded values from a valueSummary type.
   *
   * @param valueSummary A ValueSummary of which guarded values are extracted
   * @return A list of guarded values
   */
  static List<GuardedValue<?>> getGuardedValues(ValueSummary<?> valueSummary) {
    List<GuardedValue<?>> guardedValueList = new ArrayList<>();
    if (valueSummary instanceof PrimitiveVS<?>) {
      guardedValueList.addAll(((PrimitiveVS<?>) valueSummary).getGuardedValues());
      return guardedValueList;
    } else if (valueSummary instanceof TupleVS) {
      TupleVS tupleVS = (TupleVS) valueSummary;
      Guard pc = tupleVS.getUniverse();
      if (pc.isFalse()) return guardedValueList;
      int length = tupleVS.getArity();
      if (length == 0) return guardedValueList;

      Guard remaining = Guard.constTrue();
      while (!remaining.isFalse()) {
        Guard guard = remaining;
        StringBuilder value = new StringBuilder();
        boolean hasEmptyValue = false;
        for (int i = 0; i < length; i++) {
          List<GuardedValue<?>> elementGV =
              ValueSummary.getGuardedValues(tupleVS.getField(i).restrict(guard));
          if (!elementGV.isEmpty()) {
            guard = guard.and(elementGV.get(0).getGuard());
            value.append(elementGV.get(0).getValue());
          } else {
            hasEmptyValue = true;
            break;
          }
          value.append(", ");
        }
        if (!hasEmptyValue) {
          guardedValueList.add(new GuardedValue<>(value.toString(), guard));
        }
        remaining = remaining.and(guard.not());
      }
      return guardedValueList;
    } else if (valueSummary instanceof NamedTupleVS) {
      return ValueSummary.getGuardedValues(((NamedTupleVS) valueSummary).getTuple());
    } else if (valueSummary instanceof ListVS<?>) {
    } else if (valueSummary instanceof MapVS<?, ?, ?>) {
    } else if (valueSummary instanceof SetVS<?>) {
    } else if (valueSummary instanceof UnionVS) {
    } else if (valueSummary instanceof Message) {
    }
    throw new RuntimeException(
        "Fetching guarded values for composite types is unsupported. Ensure map keys are of primitive, tuple, or named tuple type.");
  }

  /**
   * Check whether a value summary has any values under any path condition
   *
   * @return Whether the path condition is empty
   */
  boolean isEmptyVS();

  /**
   * Restrict the value summary's universe with a provided guard
   *
   * @param guard The guard to conjoin to the current value summary's universe
   * @return The result of restricting the value summary's universe
   */
  T restrict(Guard guard);

  /**
   * Merge the value summary with other provided value summaries
   *
   * @param summaries The summaries to merge the value summary with
   * @return The result of the merging
   */
  T merge(Iterable<T> summaries);

  /**
   * Merge the value summary with another value summary
   *
   * @param summary The summary to merge the value summary with
   * @return The result of the merging
   */
  T merge(T summary);

  /**
   * Create a new value summary that is equal to the `update` value under the given `guard` and same
   * as the old value otherwise `!guard`
   *
   * @param guard The condition under which the value summary should be updated
   * @param updateVal The value to update the value summary to
   * @return The result of the update
   */
  T updateUnderGuard(Guard guard, T updateVal);

  /**
   * Check whether the value summary is equal to another value summary
   *
   * @param cmp The other value summary
   * @param guard The guard for the universe of the equality result
   * @return Boolean VS representing the guard under which the two value summaries are equal
   */
  PrimitiveVS<Boolean> symbolicEquals(T cmp, Guard guard);

  /**
   * Get the Guard that represents the universe of the value summary Disjunction of the guards of
   * all the guarded values
   *
   * @return The universe of the value summary
   */
  Guard getUniverse();

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  T getCopy();

  /**
   * Permute the value summary
   *
   * @param m1 first machine
   * @param m2 second machine
   * @return A new cloned copy of the value summary with m1 and m2 swapped
   */
  T swap(Machine m1, Machine m2);

  /**
   * String representation of the value summary
   *
   * @return A string
   */
  String toString();

  /**
   * Detailed string representation of the value summary
   *
   * @return A string
   */
  String toStringDetailed();

  /**
   * Compute a concrete hash code
   *
   * @return An int
   */
  int computeConcreteHash();

  /**
   * Get a concrete hash code
   *
   * @return An int
   */
  int getConcreteHash();
}
