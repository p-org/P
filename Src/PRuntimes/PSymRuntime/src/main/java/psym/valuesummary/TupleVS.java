package psym.valuesummary;

import java.util.Arrays;
import java.util.Collections;
import java.util.List;
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

  /** The fields of the tuple */
  private final ValueSummary[] fields;
  /** The types of the fields of the tuple */
  private final Class[] classes;

  /**
   * Copy-constructor for TupleVS
   *
   * @param fields Fields of the tuple
   * @param classes Types of the fields of the tuple
   */
  public TupleVS(ValueSummary[] fields, Class[] classes) {
    this.fields = fields;
    this.classes = classes;
    this.concreteHash = computeConcreteHash();
  }

  /**
   * Copy-constructor for TupleVS
   *
   * @param old The TupleVS to copy
   */
  public TupleVS(TupleVS old) {
    this(
        Arrays.copyOf(old.fields, old.fields.length),
        Arrays.copyOf(old.classes, old.classes.length));
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
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public TupleVS getCopy() {
    return new TupleVS(this);
  }

  /**
   * Permute the value summary
   *
   * @param m1 first machine
   * @param m2 second machine
   * @return A new cloned copy of the value summary with m1 and m2 swapped
   */
  public TupleVS swap(Machine m1, Machine m2) {
    return new TupleVS(
        Arrays.stream(this.fields).map(x -> x.swap(m1, m2)).toArray(size -> new ValueSummary[size]),
        Arrays.copyOf(this.classes, this.classes.length));
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
    if (guard.equals(getUniverse())) return new TupleVS(this);

    ValueSummary<?>[] resultFields = new ValueSummary[fields.length];
    for (int i = 0; i < fields.length; i++) {
      resultFields[i] = fields[i].restrict(guard);
    }
    return new TupleVS(resultFields);
  }

  @Override
  public TupleVS merge(Iterable<TupleVS> summaries) {
    List<ValueSummary> resultList = Arrays.asList(fields);
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
    return fields[0].getUniverse();
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
