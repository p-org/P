package psym.valuesummary;

import com.google.common.collect.ImmutableList;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;
import java.util.stream.IntStream;
import lombok.Getter;
import psym.runtime.machine.Machine;
import psym.utils.exception.BugFoundException;

/** Represents the list value summaries. */
public class ListVS<T extends ValueSummary<T>> implements ValueSummary<ListVS<T>> {
  @Getter
  /** Concrete hash used for hashing in explicit-state search */
  private final int concreteHash;
  @Getter
  /** Concrete value used in explicit-state search */
  private final List<Object> concreteValue;

  /** The size of the list under all guards */
  private final PrimitiveVS<Integer> size;
  /**
   * The contents of the list, where T is a value summary itself, value summary at index i
   * represents the possible content at that index
   */
  @Getter private final List<T> items;

  public ListVS(PrimitiveVS<Integer> size, List<T> items) {
    this.size = size;
    this.items = items;
    this.concreteHash = computeConcreteHash();
    this.concreteValue = computeConcreteValue();
  }

  /**
   * Make a new ListVS with the specified universe
   *
   * @param universe The universe for the new ListVS
   */
  public ListVS(Guard universe) {
    this(new PrimitiveVS<>(0).restrict(universe), new ArrayList<>());
  }

  /**
   * Copy-constructor for ListVS
   *
   * @param old The ListVS to copy
   */
  public ListVS(ListVS<T> old) {
    this(new PrimitiveVS<>(old.size), new ArrayList<>(old.items));
  }

  /**
   * Copy the value summary
   *
   * @return A new cloned copy of the value summary
   */
  public ListVS<T> getCopy() {
    return new ListVS(this);
  }

  public ListVS<T> swap(Map<Machine, Machine> mapping) {
    return new ListVS(
        new PrimitiveVS<>(this.size),
        this.items.stream().map(i -> i.swap(mapping)).collect(Collectors.toList()));
  }

  /**
   * Is the list empty?
   *
   * @return Whether the list is empty or not
   */
  public boolean isEmpty() {
    return isEmptyVS() || !IntegerVS.hasPositiveValue(size);
  }

  /**
   * Returns the size of the list value summary
   *
   * @return Integer VS representing size under different guards
   */
  public PrimitiveVS<Integer> size() {
    return size;
  }

  /** Is the value summary empty with no values in it */
  @Override
  public boolean isEmptyVS() {
    return items.isEmpty() || size.isEmptyVS();
  }

  /**
   * Restrict the universe of the list value summary
   *
   * @param guard The guard to conjoin to the current value summary's universe
   * @return Restricted value summary
   */
  @Override
  public ListVS<T> restrict(Guard guard) {
    // if the guard used for restriction is same as the universe then we can ignore this restrict
    // operation as a no-op
    if (guard.equals(getUniverse())) return new ListVS<>(this);

    final PrimitiveVS<Integer> newSize = size.restrict(guard);
    final List<T> newItems = new ArrayList<>();

    Integer max = IntegerVS.maxValue(newSize);
    if (max != null) {
      for (int i = 0; i < max; i++) {
        T newItem = this.items.get(i).restrict(guard);
        assert (newItem.getUniverse().implies(newSize.getUniverse()).isTrue());
        newItems.add(newItem);
      }
    }
    return new ListVS<>(newSize, newItems);
  }

  @Override
  public ListVS<T> updateUnderGuard(Guard guard, ListVS<T> updatedVal) {
    return this.restrict(guard.not()).merge(ImmutableList.of(updatedVal.restrict(guard)));
  }

  @Override
  public ListVS<T> merge(Iterable<ListVS<T>> summaries) {
    final List<PrimitiveVS<Integer>> sizesToMerge = new ArrayList<>();
    final List<List<T>> itemsToMergeByIndex = new ArrayList<>();

    // first add this list's items to the itemsToMergeByIndex
    for (T item : this.items) {
      itemsToMergeByIndex.add(new ArrayList<>(Collections.singletonList(item)));
    }

    for (ListVS<T> summary : summaries) {
      sizesToMerge.add(summary.size);

      for (int i = 0; i < summary.items.size(); i++) {
        if (i < itemsToMergeByIndex.size()) {
          itemsToMergeByIndex.get(i).add(summary.items.get(i));
        } else {
          itemsToMergeByIndex.add(new ArrayList<>(Collections.singletonList(summary.items.get(i))));
        }
      }
    }

    final PrimitiveVS<Integer> mergedSize = size.merge(sizesToMerge);

    final List<T> mergedItems = new ArrayList<>();

    for (List<T> itemsToMerge : itemsToMergeByIndex) {
      final T mergedItem = itemsToMerge.get(0).merge(itemsToMerge.subList(1, itemsToMerge.size()));
      mergedItems.add(mergedItem);
    }

    return new ListVS<>(mergedSize, mergedItems);
  }

  @Override
  public ListVS<T> merge(ListVS<T> summary) {
    return merge(Collections.singletonList(summary));
  }

  @Override
  public PrimitiveVS<Boolean> symbolicEquals(ListVS<T> cmp, Guard pc) {
    if (cmp == null) {
      return BooleanVS.trueUnderGuard(Guard.constFalse());
    }

    // check if size is empty
    if (size.isEmptyVS()) {
      if (cmp.isEmptyVS()) {
        return BooleanVS.trueUnderGuard(pc);
      } else {
        return BooleanVS.trueUnderGuard(Guard.constFalse());
      }
    }

    // check if each item in the list is symbolically equal
    Guard equalCond = BooleanVS.getTrueGuard(this.size.symbolicEquals(cmp.size, pc));
    for (GuardedValue<Integer> size : this.size.getGuardedValues()) {
      if (cmp.size.hasValue(size.getValue())) {
        Guard finalEqualCond = equalCond;
        Guard listEqual =
            IntStream.range(0, size.getValue())
                .mapToObj(
                    (i) ->
                        this.items
                            .get(i)
                            .symbolicEquals(cmp.items.get(i), finalEqualCond)
                            .getGuardFor(true))
                .reduce(Guard::and)
                .orElse(Guard.constTrue());
        equalCond = equalCond.and(listEqual);
      }
    }

    return BooleanVS.trueUnderGuard(equalCond).restrict(getUniverse().and(cmp.getUniverse()));
  }

  @Override
  public Guard getUniverse() {
    return size.getUniverse();
  }

  /**
   * Add an item to the List
   *
   * @param item The Item value summary to be add to the ListVS.
   * @return The updated ListVS
   */
  public ListVS<T> add(T item) {
    // assert(Checks.includedIn(item.getUniverse(), getUniverse()));
    PrimitiveVS<Integer> newSize =
        size.updateUnderGuard(item.getUniverse(), IntegerVS.add(size, 1));
    final List<T> newItems = new ArrayList<>(this.items);

    for (GuardedValue<Integer> possibleSize :
        this.size.restrict(item.getUniverse()).getGuardedValues()) {
      final int sizeValue = possibleSize.getValue();
      final T guardedItemToAdd = item.restrict(possibleSize.getGuard());

      if (sizeValue == newItems.size()) {
        newItems.add(guardedItemToAdd);
      } else {
        newItems.set(
            sizeValue,
            newItems.get(sizeValue).updateUnderGuard(possibleSize.getGuard(), guardedItemToAdd));
      }
    }
    // assert(Checks.sameUniverse(this.getUniverse(), newListVS.getUniverse()));
    return new ListVS<>(newSize, newItems);
  }

  /**
   * Is index in range?
   *
   * @param indexSummary The index to check
   */
  public PrimitiveVS<Boolean> inRange(PrimitiveVS<Integer> indexSummary) {
    return BooleanVS.and(
        IntegerVS.lessThan(indexSummary, size), IntegerVS.lessThan(-1, indexSummary));
  }

  /**
   * Is an index in range?
   *
   * @param index The index to check
   */
  public PrimitiveVS<Boolean> inRange(int index) {
    return BooleanVS.and(IntegerVS.lessThan(index, size), -1 < index);
  }

  /**
   * Get an item from the ListVS
   *
   * @param indexSummary The index to take from the ListVS. Should be possible under a subset of the
   *     ListVS's conditions.
   */
  public T get(PrimitiveVS<Integer> indexSummary) {
    // assert(Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
    assert (!indexSummary.isEmptyVS());
    return this.restrict(indexSummary.getUniverse()).getHelper(indexSummary);
  }

  public T getHelper(PrimitiveVS<Integer> indexSummary) {
    // (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
    final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
    // make sure it is always in range
    Guard outOfRange = inRange.getGuardFor(false);
    if (!outOfRange.isFalse()) {
      // there is a possibility that the index is out-of-bounds
      throw new BugFoundException(
              "Index was out of range. Must be non-negative and less than the size of the collection.",
              outOfRange);
    }

    T merger = null;
    List<T> toMerge = new ArrayList<>();
    // for each possible index value
    for (GuardedValue<Integer> index : indexSummary.getGuardedValues()) {
      assert (items.size() > index.getValue());
      T item = items.get(index.getValue()).restrict(index.getGuard());
      if (merger == null) merger = item;
      else toMerge.add(item);
    }

    return merger != null ? merger.merge(toMerge) : null;
  }

  /**
   * Set an item in the ListVS
   *
   * @param indexSummary The index in the list to set in the ListVS. Should be possible under a
   *     subset of the ListVS's conditions.
   * @param itemToSet The item to put in the ListVS. Should be possible under a subset of the
   *     ListVS's conditions.
   * @return The resultant ListVS
   */
  public ListVS<T> set(PrimitiveVS<Integer> indexSummary, T itemToSet) {
    ListVS<T> restrictedList = this.restrict(indexSummary.getUniverse());
    if (restrictedList.getUniverse().isFalse()) return this;
    return updateUnderGuard(
        indexSummary.getUniverse(), restrictedList.setHelper(indexSummary, itemToSet));
  }

  /**
   * Set an item in the ListVS
   *
   * @param indexSummary The index to set in the ListVS. Should be possible under the same
   *     conditions as the ListVS.
   * @param itemToSet The item to put in the ListVS. Should be possible under the same conditions as
   *     the ListVS.
   * @return The result of setting the ListVS
   */
  private ListVS<T> setHelper(PrimitiveVS<Integer> indexSummary, T itemToSet) {
    final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
    Guard outOfRange = inRange.getGuardFor(false);
    // make sure it is always in range
    if (!outOfRange.isFalse()) {
      // there is a possibility that the index is out-of-bounds
      throw new BugFoundException(
              "Index was out of range. Must be non-negative and less than the size of the collection.",
          outOfRange);
    }

    ListVS<T> merger = null;
    List<ListVS<T>> toMerge = new ArrayList<>();
    // for each possible index value
    for (GuardedValue<Integer> index : indexSummary.getGuardedValues()) {
      final List<T> newItems = new ArrayList<>(items);
      // the original item is updated when this is the index (i.e., index.guard holds)
      final T newEntry =
          newItems.get(index.getValue()).updateUnderGuard(index.getGuard(), itemToSet);
      newItems.set(index.getValue(), newEntry);
      ListVS<T> newList = new ListVS<>(size, newItems).restrict(index.getGuard());
      if (merger == null) merger = newList;
      else toMerge.add(newList);
    }

    assert merger != null;
    return merger.merge(toMerge);
  }

  /**
   * Check whether the ListVS contains an element
   *
   * @param element The element to check for. Should be possible under a subset of the ListVS's
   *     conditions.
   * @return Whether or not the ListVS contains an element
   */
  public PrimitiveVS<Boolean> contains(T element) {
    return BooleanVS.trueUnderGuard(indexOf(element).getUniverse()).restrict(this.getUniverse());
  }

  /**
   * Insert an item in the ListVS.
   *
   * @param indexSummary The index to insert at in the ListVS. Should be possible under a subset of
   *     the ListVS's conditions.
   * @param itemToInsert The item to put in the ListVS. Should be possible under the same subset of
   *     the ListVS's conditions.
   * @return The result of inserting into the ListVS
   */
  public ListVS<T> insert(PrimitiveVS<Integer> indexSummary, T itemToInsert) {
    if (indexSummary.getUniverse().isFalse()) return this;

    Guard addUniverse = IntegerVS.equalTo(indexSummary, size()).getGuardFor(true);
    if (!addUniverse.isFalse()) {
      return this.add(itemToInsert.restrict(addUniverse))
          .insert(
              indexSummary.restrict(addUniverse.not()), itemToInsert.restrict(addUniverse.not()));
    }

    final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
    // make sure it is always in range
    Guard outOfRange = inRange.getGuardFor(false);
    if (!outOfRange.isFalse()) {
      // there is a possibility that the index is out-of-bounds
      throw new BugFoundException(
              "Index must be within the bounds of the List.",
              outOfRange);
    }

    // 1. add a new entry (we'll re-add the last entry)
    ListVS<T> newList = new ListVS<>(this);
    newList =
        newList.add(newList.get(IntegerVS.subtract(size, 1)).restrict(indexSummary.getUniverse()));

    // 2. setting at the insertion index
    PrimitiveVS<Integer> current = indexSummary;
    T prev = newList.get(current);
    newList = newList.set(indexSummary, itemToInsert);
    current = IntegerVS.add(current, 1);

    // 3. setting everything after insertion index to be the previous element
    while (BooleanVS.isEverTrue(IntegerVS.lessThan(current, size))) {
      Guard guard = BooleanVS.getTrueGuard(IntegerVS.lessThan(current, size));
      T old = this.restrict(guard).get(current.restrict(guard));
      newList = newList.set(current.restrict(guard), prev.restrict(guard));
      prev = old;
      current = IntegerVS.add(current, 1);
    }

    return newList;
  }

  /**
   * Remove an item from the ListVS.
   *
   * @param indexSummary The index to remove from in the ListVS. Should be possible under a subset
   *     of the ListVS's conditions.
   * @return The result of removing from the ListVS
   */
  public ListVS<T> removeAt(PrimitiveVS<Integer> indexSummary) {
    // assert (Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
    ListVS<T> guarded = this.restrict(indexSummary.getUniverse());
    ListVS<T> removed = guarded.removeAtHelper(indexSummary);
    return updateUnderGuard(indexSummary.getUniverse(), removed);
  }

  /**
   * Remove an item from the ListVS.
   *
   * @param indexSummary The index to remove from in the ListVS. Should be possible under the same
   *     conditions as the ListVS.
   * @return The result of removing from the ListVS
   */
  private ListVS<T> removeAtHelper(PrimitiveVS<Integer> indexSummary) {
    // assert (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
    final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
    // make sure it is always in range
    Guard outOfRange = inRange.getGuardFor(false);
    if (!outOfRange.isFalse()) {
      // there is a possibility that the index is out-of-bounds
      throw new BugFoundException(
              "Index was out of range. Must be non-negative and less than the size of the collection.",
              outOfRange);
    }

    // new size
    PrimitiveVS<Integer> newSize = IntegerVS.subtract(size, 1);

    /* Optimize case where the index can only take on one value */
    if (indexSummary.getValues().size() == 1) {
      int idx = indexSummary.getValues().iterator().next();
      List<T> newItems = new ArrayList<T>(items);
      newItems.remove(idx);
      return new ListVS<>(newSize, newItems);
    }

    ListVS<T> newList = new ListVS<>(newSize, items.subList(0, items.size() - 1));
    PrimitiveVS<Integer> current = indexSummary;

    // Setting everything after removal index to be the next element
    while (BooleanVS.isEverTrue(IntegerVS.lessThan(IntegerVS.add(current, 1), size))) {
      // ScheduleLogger.log("removeAt while " + current);
      Guard thisCond = BooleanVS.getTrueGuard(IntegerVS.lessThan(IntegerVS.add(current, 1), size));
      current = current.restrict(thisCond);
      T next = this.get(IntegerVS.add(current, 1));
      newList = newList.set(current, next);
      current = IntegerVS.add(current, 1);
    }
    return newList;
  }

  /**
   * Get the index of an element in the ListVS
   *
   * @param element The element to find the index for.
   * @return The Integer VS representing index of the element
   */
  public PrimitiveVS<Integer> indexOf(T element) {
    // assert(Checks.includedIn(element.getUniverse(), getUniverse()));
    if (element.getUniverse().isFalse()) {
      return new PrimitiveVS<>();
    }
    // System.out.println(this.guard(element.getUniverse()));
    PrimitiveVS<Integer> i = new PrimitiveVS<>(0).restrict(element.getUniverse());

    PrimitiveVS<Integer> index = new PrimitiveVS<>();
    ListVS<T> restrictedList = this.restrict(element.getUniverse());

    while (BooleanVS.isEverTrue(IntegerVS.lessThan(i, restrictedList.size))) {
      Guard cond = BooleanVS.getTrueGuard(IntegerVS.lessThan(i, restrictedList.size));
      Guard contains =
          BooleanVS.getTrueGuard(
              element.restrict(cond).symbolicEquals(restrictedList.get(i.restrict(cond)), cond));
      index = index.merge(i.restrict(contains));
      i = IntegerVS.add(i, 1);
    }
    return index;
  }

  /**
   * Get the universe under which the data structure is nonempty
   *
   * @return The universe under which the data structure is nonempty
   */
  public Guard getNonEmptyUniverse() {
    if (size.getGuardedValues().size() > 1) {
      if (getUniverse().and(size.getGuardFor(0).not()).isFalse()) {
        throw new RuntimeException();
      }
    }
    return getUniverse().and(size.getGuardFor(0).not());
  }

  @Override
  public int computeConcreteHash() {
    int hashCode = 1;
    for (int i = 0; i < items.size(); i++) {
      hashCode = 31 * hashCode + (items.get(i) == null ? 0 : items.get(i).getConcreteHash());
    }
    return hashCode;
  }

  @Override
  public List<Object> computeConcreteValue() {
    List<Object> value = new ArrayList<>();
    for (int i = 0; i < items.size(); i++) {
      value.add(items.get(i) == null ? null : items.get(i).getConcreteValue());
    }
    return value;
  }

  @Override
  public String toString() {
    StringBuilder out = new StringBuilder();
    out.append("List[");
    List<GuardedValue<Integer>> guardedSizeList = size.getGuardedValues();
    for (int j = 0; j < guardedSizeList.size(); j++) {
      GuardedValue<Integer> guardedSize = guardedSizeList.get(j);
      out.append("  #").append(guardedSize.getValue()).append(": [");
      for (int i = 0; i < guardedSize.getValue(); i++) {
        out.append(this.items.get(i).restrict(guardedSize.getGuard()));
        if (i < guardedSize.getValue() - 1) {
          out.append(", ");
        }
      }
      out.append("]");
      if (j < guardedSizeList.size() - 1) {
        out.append(",");
      }
    }
    out.append("]");
    return out.toString();
  }

  public String toStringDetailed() {
    StringBuilder out = new StringBuilder();
    out.append("List[");
    List<GuardedValue<Integer>> guardedSizeList = size.getGuardedValues();
    for (int j = 0; j < guardedSizeList.size(); j++) {
      GuardedValue<Integer> guardedSize = guardedSizeList.get(j);
      out.append("  #").append(guardedSize.getValue()).append(": [");
      for (int i = 0; i < guardedSize.getValue(); i++) {
        out.append(this.items.get(i).restrict(guardedSize.getGuard()).toStringDetailed())
            .append(", ");
      }
      out.append(",");
    }
    out.append("]");
    return out.toString();
  }
}
