package psym.valuesummary;

import java.util.*;
import lombok.Getter;
import psym.runtime.machine.Machine;

/** Class for set value summaries */
public class SetVS<T extends ValueSummary<T>> implements ValueSummary<SetVS<T>> {
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;
  @Getter
  /** Concrete value used in explicit-state search */
  private final Set<Object> concreteValue;

  /** The underlying set */
  private final ListVS<T> elements;

  public SetVS(ListVS<T> elements) {
    this.elements = elements;
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  public SetVS(Guard universe) {
    this.elements = new ListVS<>(universe);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Copy-constructor for SetVS
   *
   * @param old The SetVS to copy
   */
  public SetVS(SetVS<T> old) {
    this.elements = new ListVS<>(old.elements);
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /** Get all the different possible guarded values */
  public ListVS<T> getElements() {
    return elements;
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public SetVS<T> getCopy() {
    return new SetVS(this);
  }

  public SetVS<T> swap(Map<Machine, Machine> mapping) {
    return new SetVS<T>(this.elements.swap(mapping));
  }

  public PrimitiveVS<Integer> size() {
    return elements.size();
  }

  public boolean isEmpty() {
    return elements.isEmpty();
  }

  @Override
  public boolean isEmptyVS() {
    return elements.isEmptyVS();
  }

  @Override
  public SetVS<T> restrict(Guard guard) {
    if (guard.equals(getUniverse())) return new SetVS<T>(new ListVS<>(elements));

    return new SetVS<>(new ListVS<>(elements.restrict(guard)));
  }

  @Override
  public SetVS<T> merge(Iterable<SetVS<T>> summaries) {
    List<ListVS<T>> listsToMerge = new ArrayList<>();

    for (SetVS<T> summary : summaries) {
      listsToMerge.add(summary.elements);
    }

    return new SetVS<>(elements.merge(listsToMerge));
  }

  @Override
  public SetVS<T> merge(SetVS<T> summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public SetVS<T> updateUnderGuard(Guard guard, SetVS<T> update) {
    return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(SetVS<T> cmp, Guard pc) {
    if (cmp == null) {
      return BooleanVS.trueUnderGuard(Guard.constFalse());
    }

    // check if size is empty
    if (elements.size().isEmptyVS()) {
      if (cmp.isEmptyVS()) {
        return BooleanVS.trueUnderGuard(pc);
      } else {
        return BooleanVS.trueUnderGuard(Guard.constFalse());
      }
    }

    // check if each item in the set is symbolically equal
    Guard equalCond = pc;
    for (T lhs : this.elements.getItems()) {
      equalCond = equalCond.and(cmp.contains(lhs).getGuardFor(true));
    }
    for (T rhs : cmp.elements.getItems()) {
      equalCond = equalCond.and(this.contains(rhs).getGuardFor(true));
    }
    // check case where both empty
    Guard thisEmpty = this.elements.size().getGuardFor(0);
    if (!thisEmpty.isFalse()) {
      Guard cmpEmpty = cmp.elements.size().getGuardFor(0);
      equalCond = equalCond.or(thisEmpty.and(cmpEmpty));
    }

    return BooleanVS.trueUnderGuard(equalCond).restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    return elements.getUniverse();
  }

  /**
   * Check whether the SetVS contains an element
   *
   * @param element The element to check for. Should be possible under a subset of the SetVS's
   *     conditions.
   * @return Whether or not the SetVS contains an element
   */
  public PrimitiveVS<Boolean> contains(T element) {
    if (element.getUniverse().isFalse()) {
      return new PrimitiveVS<>();
    }

    // check if each item in the set is symbolically equal
    Guard cond = element.getUniverse().and(getUniverse());

    Guard containsCond = Guard.constFalse();
    for (T lhs : this.elements.getItems()) {
      containsCond = containsCond.or(BooleanVS.getTrueGuard(element.symbolicEquals(lhs, cond)));
    }

    return BooleanVS.trueUnderGuard(containsCond).restrict(cond);
  }

  /**
   * Get the universe under which the data structure is nonempty
   *
   * @return The universe under which the data structure is nonempty
   */
  public Guard getNonEmptyUniverse() {
    return elements.getNonEmptyUniverse();
  }

  /**
   * Add an item to the SetVS.
   *
   * @param itemSummary The element to add.
   * @return The SetVS with the element added
   */
  public SetVS<T> add(T itemSummary) {
    Guard absent = contains(itemSummary.restrict(getUniverse())).getGuardFor(false);
    ListVS<T> newElements = elements.updateUnderGuard(absent, elements.add(itemSummary));
    return new SetVS<>(newElements);
  }

  /**
   * Remove an item from the SetVS if present (otherwise no op)
   *
   * @param itemSummary The element to remove. Should be possible under a subset of the SetVS's
   *     conditions.
   * @return The SetVS with the element removed.
   */
  public SetVS<T> remove(T itemSummary) {
    PrimitiveVS<Integer> idx = elements.indexOf(itemSummary);
    idx = idx.restrict(elements.inRange(idx).getGuardFor(true));
    if (idx.isEmptyVS()) return this;
    ListVS<T> newElements = elements.removeAt(idx);
    return new SetVS<>(newElements);
  }

  /**
   * Get an item from the SetVS
   *
   * @param indexSummary The index to take from the SetVS. Should be possible under a subset of the
   *     SetVS's conditions.
   */
  public T get(PrimitiveVS<Integer> indexSummary) {
    return elements.get(indexSummary);
  }

  @Override
  public int computeConcreteHash() {
    int hashCode = 1;
    List<T> allItems = new ArrayList<>(this.elements.getItems());
    allItems.sort(Comparator.comparing(ValueSummary::getConcreteHash));
    for (T item: allItems) {
      hashCode = 31 * hashCode + (item == null ? 0 : item.getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public Set<Object> computeConcreteValue() {
    Set<Object> value = new HashSet<>();
    for (T item: this.elements.getItems()) {
      value.add(item == null ? null : item.getConcreteValue());
    }
    return value;
  }

  @Override
  public String toString() {
    StringBuilder out = new StringBuilder();
    out.append("Set[");
    List<GuardedValue<Integer>> guardedSizeList = elements.size().getGuardedValues();
    for (int j = 0; j < guardedSizeList.size(); j++) {
      GuardedValue<Integer> guardedSize = guardedSizeList.get(j);
      out.append("  #").append(guardedSize.getValue()).append(": [");
      for (int i = 0; i < guardedSize.getValue(); i++) {
        out.append(this.elements.getItems().get(i).restrict(guardedSize.getGuard()));
        if (i < guardedSize.getValue() - 1) {
          out.append(", ");
        }
      }
      if (j < guardedSizeList.size() - 1) {
        out.append(",");
      }
      out.append("]");
    }
    out.append("]");
    return out.toString();
  }

  public String toStringDetailed() {
    return "Set[" + elements.toStringDetailed() + "]";
  }
}
