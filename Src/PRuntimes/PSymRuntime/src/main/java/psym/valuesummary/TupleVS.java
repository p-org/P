package psym.valuesummary;

import java.util.*;
import java.util.stream.Collectors;
import java.util.stream.IntStream;
import lombok.Getter;
import psym.runtime.machine.Machine;

/** Represents a tuple value summaries */
@SuppressWarnings("unchecked")
public class TupleVS implements ValueSummary<TupleVS> {
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;
  @Getter
  /** Concrete value used in explicit-state search */
  private final Object[] concreteValue;

  /** The fields of the tuple */
  private final ValueSummary[] fields;
  /** The types of the fields of the tuple */
  private final Class[] classes;

  /**
   * Copy-constructor for TupleVS
   *
   * @param inpFields Fields of the tuple
   * @param inpClasses Types of the fields of the tuple
   */
  public TupleVS(ValueSummary[] inpFields, Class[] inpClasses) {
    this.fields = Arrays.copyOf(inpFields, inpFields.length);
    this.classes = Arrays.copyOf(inpClasses, inpClasses.length);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
    assert (IntStream.range(0, this.fields.length).allMatch(x -> this.fields[x].getUniverse().equals(this.fields[0].getUniverse()))) :
            "Error in tuple field guards";
  }

  /**
   * Copy-constructor for TupleVS
   *
   * @param old The TupleVS to copy
   */
  public TupleVS(TupleVS old) {
    this(old.fields, old.classes);
  }

  /** Make a new TupleVS from the provided items */
  public TupleVS(ValueSummary<?>... items) {
    Guard commonGuard = Guard.constTrue();
    for (ValueSummary<?> vs : items) {
      commonGuard = commonGuard.and(vs.getUniverse());
    }
    final Guard guard = commonGuard;
    this.fields =
        Arrays.stream(items)
            .map(x -> x.restrict(guard))
            .collect(Collectors.toList())
            .toArray(new ValueSummary[items.length]);
    this.classes =
        Arrays.stream(items)
            .map(x -> x.getClass())
            .collect(Collectors.toList())
            .toArray(new Class[items.length]);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
    assert (IntStream.range(0, this.fields.length).allMatch(x -> this.fields[x].getUniverse().equals(this.fields[0].getUniverse()))) :
            "Error in tuple field guards";
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public TupleVS getCopy() {
    return new TupleVS(this);
  }

  public TupleVS swap(Map<Machine, Machine> mapping) {
    return new TupleVS(
        Arrays.stream(this.fields).map(x -> x.swap(mapping)).toArray(size -> new ValueSummary[size]),
        this.classes);
  }

  /**
   * Get the names of the TupleVS fields
   *
   * @return Array containing the names of the TupleVS fields
   */
  public String[] getNames() {
    String[] result = new String[classes.length];
    for (int i = 0; i < classes.length; i++) {
      result[i] = classes[i].toString();
    }
    return result;
  }

  /**
   * Get the arity of the TupleVS
   *
   * @return The arity of the TupleVS
   */
  public int getArity() {
    return fields.length;
  }

  /**
   * Get the i-th value in the TupleVS
   *
   * @param i The index to get from the TupleVS
   * @return The value at index i
   */
  public ValueSummary getField(int i) {
    return fields[i];
  }

  /**
   * Get the i-th class in the TupleVS
   *
   * @param i The index to get from the TupleVS
   * @return The class at index i
   */
  public Class getClass(int i) {
    return classes[i];
  }

  /**
   * Set the i-th value in the TupleVS to the provided value
   *
   * @param i The index to set in the TupleVS
   * @param val The value to set in the TupleVS
   * @return The result after updating the TupleVS
   */
  public TupleVS setField(int i, ValueSummary val) {
    final ValueSummary[] newItems = new ValueSummary[fields.length];
    System.arraycopy(fields, 0, newItems, 0, fields.length);
    if (!(val.getClass().equals(classes[i]))) throw new ClassCastException();
    newItems[i] = newItems[i].updateUnderGuard(val.getUniverse(), val);
    return new TupleVS(newItems);
  }

  @Override
  public boolean isEmptyVS() {
    // Optimization: Tuples should always be nonempty,
    // and all fields should exist under the same conditions
    return fields[0].isEmptyVS();
  }

  @Override
  public TupleVS restrict(Guard guard) {
    ValueSummary<?>[] resultFields = new ValueSummary[fields.length];
    for (int i = 0; i < fields.length; i++) {
      resultFields[i] = fields[i].restrict(guard);
    }
    return new TupleVS(resultFields);
  }

  @Override
  public TupleVS merge(Iterable<TupleVS> summaries) {
    List<ValueSummary> resultList = Arrays.asList(Arrays.copyOf(fields, fields.length));
    for (TupleVS summary : summaries) {
      for (int i = 0; i < summary.fields.length; i++) {
        if (i < resultList.size()) {
          if (summary.fields[i].getClass() != classes[i]) {
            classes[i] = UnionVS.class;
            ValueSummary lhs = resultList.get(i);
            ValueSummary rhs = summary.fields[i];
            resultList.set(
                i,
                ValueSummary.castToAny(lhs.getUniverse(), lhs)
                    .merge(ValueSummary.castToAny(rhs.getUniverse(), rhs)));
          } else {
            resultList.set(i, resultList.get(i).merge(summary.fields[i]));
          }
        } else {
          resultList.add(summary.fields[i]);
        }
      }
    }
    return new TupleVS(resultList.toArray(new ValueSummary[0]));
  }

  @Override
  public TupleVS merge(TupleVS summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public TupleVS updateUnderGuard(Guard guard, TupleVS update) {
    return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(TupleVS cmp, Guard pc) {
    if (cmp == null) {
      return BooleanVS.trueUnderGuard(Guard.constFalse());
    }

    if (fields.length != cmp.fields.length) {
      return new PrimitiveVS<>(false);
    }
    Guard tupleEqual =
        IntStream.range(0, fields.length)
            .mapToObj((i) -> fields[i].symbolicEquals(cmp.fields[i], pc).getGuardFor(true))
            .reduce(Guard::and)
            .orElse(Guard.constTrue());
    return BooleanVS.trueUnderGuard(pc.and(tupleEqual).and(getUniverse()).and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    // Optimization: Tuples should always be nonempty,
    // and all fields should exist under the same conditions
    Guard result = fields[0].getUniverse();
    return result;
  }

  @Override
  public int computeConcreteHash() {
    int hashCode = 1;
    for (int i = 0; i < classes.length; i++) {
      hashCode = 31 * hashCode + (fields[i] == null ? 0 : fields[i].getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public Object[] computeConcreteValue() {
    Object[] value = new Object[classes.length];
    for (int i = 0; i < classes.length; i++) {
      value[i] = fields[i] == null ? null : fields[i].getConcreteValue();
    }
    return value;
  }

  @Override
  public String toString() {
    StringBuilder str = new StringBuilder("( ");
    for (int i = 0; i < classes.length; i++) {
      str.append((classes[i]).cast(fields[i]).toString()).append(", ");
    }
    str.append(")");
    return str.toString();
  }

  public String toStringDetailed() {
    StringBuilder out = new StringBuilder();
    out.append("Tuple[");
    for (int i = 0; i < classes.length; i++) {
      out.append(getField(i).toStringDetailed()).append(", ");
    }
    out.append("]");
    return out.toString();
  }
}
