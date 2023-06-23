package psym.valuesummary;

import java.util.*;
import org.jetbrains.annotations.NotNull;
import psym.runtime.machine.Machine;

/** Represents a value of "any" type It stores a pair (type T, value of type T) */
@SuppressWarnings("ALL")
public class UnionVS implements ValueSummary<UnionVS> {
  /* Type of value stored in the any type variable */
  private final PrimitiveVS<UnionVStype> type;
  /* Map from the type of variable to the value summary representing the value of that type */
  private final Map<UnionVStype, ValueSummary> value;

  public UnionVS(
      @NotNull PrimitiveVS<UnionVStype> type, @NotNull Map<UnionVStype, ValueSummary> values) {
    this.type = type;
    this.value = values;
  }

  public UnionVS(Guard pc, UnionVStype type, ValueSummary values) {
    this.type = new PrimitiveVS<UnionVStype>(type).restrict(pc);
    this.value = new HashMap<>();
    // TODO: why are we not restricting the values?
    this.value.put(type, values);
    assert (this.type != null);
  }

  public UnionVS() {
    this.type = new PrimitiveVS<>();
    this.value = new HashMap<>();
  }

  /**
   * Copy-constructor for UnionVS
   *
   * @param old The UnionVS to copy
   */
  public UnionVS(UnionVS old) {
    this.type = new PrimitiveVS<>(old.type);
    this.value = new HashMap<>(old.value);
  }

  public UnionVS(ValueSummary vs) {
    if (vs == null) {
      this.type = new PrimitiveVS<>();
      this.value = new HashMap<>();
    } else {
      UnionVStype type;
      if (vs instanceof NamedTupleVS) {
        type = UnionVStype.getUnionVStype(vs.getClass(), ((NamedTupleVS) vs).getNames());
      } else if (vs instanceof TupleVS) {
        type = UnionVStype.getUnionVStype(vs.getClass(), ((TupleVS) vs).getNames());
      } else {
        type = UnionVStype.getUnionVStype(vs.getClass(), null);
      }
      this.type = new PrimitiveVS<UnionVStype>(type).restrict(vs.getUniverse());
      this.value = new HashMap<>();
      // TODO: why are we not restricting the values?
      this.value.put(type, vs);
      assert (this.type != null);
    }
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public UnionVS getCopy() {
    return new UnionVS(this);
  }

  /**
   * Permute the value summary
   *
   * @param m1 first machine
   * @param m2 second machine
   * @return A new cloned copy of the value summary with m1 and m2 swapped
   */
  public UnionVS swap(Machine m1, Machine m2) {
    Map<UnionVStype, ValueSummary> newValues = new HashMap<>(this.value);
    newValues.replaceAll((k, v) -> v.swap(m1, m2));
    return new UnionVS(this.type.getCopy(), newValues);
  }

  /**
   * Does the any type variable store a value of a particular type
   *
   * @param queryType type to be checked
   * @return true if the variable stores a value of "queryType" under some path constrain
   */
  public boolean hasType(UnionVStype queryType) {
    return !type.getGuardFor(queryType).isFalse();
  }

  /**
   * Get the types of value stored in the "any" type variable
   *
   * @return type of the variable
   */
  public PrimitiveVS<UnionVStype> getType() {
    return type;
  }

  /**
   * Get the value in the of a particular type
   *
   * @param type type of value
   * @return value
   */
  public ValueSummary getValue(UnionVStype type) {
    // TODO: Add a check that the type exists!
    return value.get(type);
  }

  public Guard getGuardFor(UnionVStype type) {
    return this.type.getGuardFor(type);
  }

  public void check() {
    for (UnionVStype type : this.type.getValues()) {
      assert getGuardFor(type).isFalse() || (getValue(type) != null);
    }
  }

  @Override
  public boolean isEmptyVS() {
    return type.isEmptyVS();
  }

  @Override
  public UnionVS restrict(Guard guard) {

    if (guard.equals(getUniverse())) return new UnionVS(this);

    final PrimitiveVS<UnionVStype> restrictedType = type.restrict(guard);
    final Map<UnionVStype, ValueSummary> restrictedValues = new HashMap<>();
    for (Map.Entry<UnionVStype, ValueSummary> entry : value.entrySet()) {
      final UnionVStype type = entry.getKey();
      final ValueSummary value = entry.getValue();
      if (!restrictedType.getGuardFor(type).isFalse()) {
        restrictedValues.put(type, value.restrict(guard));
      }
    }
    return new UnionVS(restrictedType, restrictedValues);
  }

  @Override
  public UnionVS merge(Iterable<UnionVS> summaries) {
    assert (type != null);
    final List<PrimitiveVS<UnionVStype>> typesToMerge = new ArrayList<>();
    final Map<UnionVStype, List<ValueSummary>> valuesToMerge = new HashMap<>();
    for (UnionVS union : summaries) {
      typesToMerge.add(union.type);
      for (Map.Entry<UnionVStype, ValueSummary> entry : union.value.entrySet()) {
        valuesToMerge
            .computeIfAbsent(entry.getKey(), (key) -> new ArrayList<>())
            .add(entry.getValue());
      }
    }

    if (valuesToMerge.size() == 0) return new UnionVS(this);

    final PrimitiveVS<UnionVStype> mergedType = type.merge(typesToMerge);
    final Map<UnionVStype, ValueSummary> mergedValue = new HashMap<>(this.value);

    for (Map.Entry<UnionVStype, List<ValueSummary>> entry : valuesToMerge.entrySet()) {
      UnionVStype type = entry.getKey();
      List<ValueSummary> value = entry.getValue();
      if (value.size() > 0) {
        ValueSummary oldValue = this.value.get(type);
        ValueSummary newValue;
        if (oldValue == null) {
          newValue = value.get(0).merge(value.subList(1, entry.getValue().size()));
        } else {
          newValue = oldValue.merge(value);
        }
        mergedValue.put(type, newValue);
      }
    }
    return new UnionVS(mergedType, mergedValue);
  }

  @Override
  public UnionVS merge(UnionVS summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public UnionVS updateUnderGuard(Guard guard, UnionVS updateVal) {
    return this.restrict(guard.not()).merge(updateVal.restrict(guard));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(UnionVS cmp, Guard pc) {
    assert (type != null);
    if (cmp == null) {
      return BooleanVS.trueUnderGuard(pc.and(getUniverse().not()));
    }
    PrimitiveVS res = type.symbolicEquals(cmp.type, pc);
    for (Map.Entry<UnionVStype, ValueSummary> payload : cmp.value.entrySet()) {
      if (!value.containsKey(payload.getKey())) {
        PrimitiveVS<Boolean> bothLackKey =
            BooleanVS.trueUnderGuard(pc.and(type.getGuardFor(payload.getKey()).not()));
        res = BooleanVS.and(res, bothLackKey);
      } else {
        res =
            BooleanVS.and(res, payload.getValue().symbolicEquals(value.get(payload.getKey()), pc));
      }
    }
    if (res.isEmptyVS()) {
      return BooleanVS.trueUnderGuard(Guard.constFalse());
    }
    return res.restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    return type.getUniverse();
  }

  public Guard getUniverse(UnionVStype type) {
    return this.type.getGuardFor(type);
  }

  @Override
  public int getConcreteHash() {
    int hashCode = 1;
    for (Map.Entry<UnionVStype, ValueSummary> entry : value.entrySet()) {
      hashCode = 31 * hashCode + (entry.getKey() == null ? 0 : entry.getKey().hashCode());
      hashCode =
          31 * hashCode + (entry.getValue() == null ? 0 : entry.getValue().getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public String toString() {
    StringBuilder out = new StringBuilder();
    out.append("[");
    for (UnionVStype type : type.getValues()) {
      out.append(value.get(type).toString());
      out.append(", ");
    }
    out.append("]");
    return out.toString();
  }

  public String toStringDetailed() {
    StringBuilder out = new StringBuilder();
    out.append("Union[");
    for (UnionVStype type : type.getValues()) {
      out.append(value.get(type).toStringDetailed()).append(", ");
    }
    out.append("]");
    return out.toString();
  }
}
