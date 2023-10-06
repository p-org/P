package psym.valuesummary;

import java.util.*;
import lombok.Getter;
import org.jetbrains.annotations.NotNull;
import psym.runtime.machine.Machine;

/** Represents a value of "any" type It stores a pair (type T, value of type T) */
@SuppressWarnings("ALL")
public class UnionVS implements ValueSummary<UnionVS> {
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;
  @Getter
  /** Concrete value used in explicit-state search */
  private final Map<UnionVStype, Object> concreteValue;

  /* Type of value stored in the any type variable */
  private final PrimitiveVS<UnionVStype> type;
  /* Map from the type of variable to the value summary representing the value of that type */
  private final Map<UnionVStype, ValueSummary> value;

  public UnionVS(
      @NotNull PrimitiveVS<UnionVStype> type, @NotNull Map<UnionVStype, ValueSummary> values) {
    this.type = type;
    this.value = values;
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  public UnionVS(Guard pc, UnionVStype type, ValueSummary values) {
    this.type = new PrimitiveVS<UnionVStype>(type).restrict(pc);
    this.value = new HashMap<>();
    // TODO: why are we not restricting the values?
    this.value.put(type, values);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  public UnionVS() {
    UnionVStype type = UnionVStype.getUnionVStype(PrimitiveVS.class, null);
    this.type = new PrimitiveVS<>(type);
    this.value = new HashMap<>();
    this.value.put(type, new PrimitiveVS(null, Guard.constTrue()));
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Copy-constructor for UnionVS
   *
   * @param old The UnionVS to copy
   */
  public UnionVS(UnionVS old) {
    this.type = new PrimitiveVS<>(old.type);
    this.value = new HashMap<>(old.value);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  public UnionVS(ValueSummary vs) {
    if (vs == null) {
      UnionVStype type = UnionVStype.getUnionVStype(PrimitiveVS.class, null);
      this.type = new PrimitiveVS<>(type);
      this.value = new HashMap<>();
      this.value.put(type, null);
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
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public UnionVS getCopy() {
    return new UnionVS(this);
  }

  public UnionVS swap(Map<Machine, Machine> mapping) {
    Map<UnionVStype, ValueSummary> newValues = new HashMap<>(this.value);
    newValues.replaceAll((k, v) -> v.swap(mapping));
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
    ValueSummary result = value.get(type);
    return result;
  }

  public Guard getGuardFor(UnionVStype type) {
    return this.type.getGuardFor(type);
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
      ValueSummary value = entry.getValue();
      if (!restrictedType.getGuardFor(type).isFalse()) {
        if (value != null) {
          value = value.restrict(guard);
        }
        restrictedValues.put(type, value);
      }
    }
    return new UnionVS(restrictedType, restrictedValues);
  }

  @Override
  public UnionVS merge(Iterable<UnionVS> summaries) {
    assert (type != null);
    final List<PrimitiveVS<UnionVStype>> typesToMerge = new ArrayList<>();
    final Map<UnionVStype, List<ValueSummary>> valuesToMerge = new HashMap<>();

    for (Map.Entry<UnionVStype, ValueSummary> entry : this.value.entrySet()) {
      if (!valuesToMerge.containsKey(entry.getKey())) {
        valuesToMerge.put(entry.getKey(), new ArrayList<>());
      }
      if (entry.getValue() != null) {
        valuesToMerge.get(entry.getKey()).add(entry.getValue());
      }
    }

    for (UnionVS union : summaries) {
      typesToMerge.add(union.type);
      for (Map.Entry<UnionVStype, ValueSummary> entry : union.value.entrySet()) {
        if (!valuesToMerge.containsKey(entry.getKey())) {
            valuesToMerge.put(entry.getKey(), new ArrayList<>());
        }
        if (entry.getValue() != null) {
          valuesToMerge.get(entry.getKey()).add(entry.getValue());
        }
      }
    }

    final PrimitiveVS<UnionVStype> mergedType = type.merge(typesToMerge);
    final Map<UnionVStype, ValueSummary> mergedValue = new HashMap<>();

    for (Map.Entry<UnionVStype, List<ValueSummary>> entry : valuesToMerge.entrySet()) {
      UnionVStype type = entry.getKey();
      List<ValueSummary> value = entry.getValue();
      ValueSummary newValue = null;
      if (value.size() > 0) {
        newValue = value.get(0);
        if (value.size() > 1) {
          newValue = newValue.merge(value.subList(1, entry.getValue().size()));
        }
      }
      mergedValue.put(type, newValue);
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
  public PrimitiveVS<Boolean> symbolicEquals(UnionVS cmp_orig, Guard pc) {
    UnionVS cmp;
    if (cmp_orig == null) {
      cmp = new UnionVS();
    } else {
      cmp = cmp_orig;
    }

    Guard equalCond = Guard.constFalse();
    for (GuardedValue<UnionVStype> typeGv: type.restrict(pc).getGuardedValues()) {
        Guard typeGuard = typeGv.getGuard();
        UnionVStype typeVal = typeGv.getValue();

        ValueSummary lhsVal = getValue(typeVal);
        ValueSummary rhsVal = cmp.getValue(typeVal);

        if (lhsVal == null && rhsVal == null) {
          equalCond = equalCond.or(typeGuard);
        } else if (lhsVal == null) {
          equalCond = equalCond.or(rhsVal.symbolicEquals(null, typeGuard).getGuardFor(true));
        } else if (rhsVal == null) {
          equalCond = equalCond.or(lhsVal.symbolicEquals(null, typeGuard).getGuardFor(true));
        } else {
          equalCond = equalCond.or(lhsVal.symbolicEquals(rhsVal, typeGuard).getGuardFor(true));
        }
    }
    return BooleanVS.trueUnderGuard(equalCond).restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    return type.getUniverse();
  }

  public Guard getUniverse(UnionVStype type) {
    return this.type.getGuardFor(type);
  }

  @Override
  public int computeConcreteHash() {
    int hashCode = 1;
    for (Map.Entry<UnionVStype, ValueSummary> entry : value.entrySet()) {
      hashCode = 31 * hashCode + (entry.getKey() == null ? 0 : entry.getKey().hashCode());
      hashCode =
          31 * hashCode + (entry.getValue() == null ? 0 : entry.getValue().getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public Map<UnionVStype, Object> computeConcreteValue() {
    Map<UnionVStype, Object> ret = new HashMap<>();
    for (Map.Entry<UnionVStype, ValueSummary> entry : value.entrySet()) {
      ret.put(entry.getKey(), entry.getValue() == null ? null : entry.getValue().getConcreteValue());
    }
    return ret;
  }

  @Override
  public String toString() {
    StringBuilder out = new StringBuilder();
    out.append("[");
    for (UnionVStype type : type.getValues()) {
      if (value.get(type) == null) {
        continue;
      }
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
      if (value.get(type) == null) {
        continue;
      }
      out.append(value.get(type).toStringDetailed()).append(", ");
    }
    out.append("]");
    return out.toString();
  }
}
