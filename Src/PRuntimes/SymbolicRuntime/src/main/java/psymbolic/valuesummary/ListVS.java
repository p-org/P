package psymbolic.valuesummary;

import com.google.common.collect.ImmutableList;
import psymbolic.runtime.ScheduleLogger;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.stream.IntStream;

/**
 * Represents the list value summaries.
 * TODO: Add comments about how the list value summary has been implemented
 * */
public class ListVS<T extends ValueSummary<T>> implements ValueSummary<ListVS<T>> {
    /** The size of the list under all guards*/
    private final PrimitiveVS<Integer> size;
    /** The contents of the list, list of value summaries*/
    private final List<T> items;

    private ListVS(PrimitiveVS<Integer> size, List<T> items) {
        this.size = size;
        this.items = items;
    }

    /**
     * Make a new ListVS with the specified universe
     * @param universe The universe for the new ListVS
     */
    public ListVS(Guard universe) {
        this(new PrimitiveVS<>(0).restrict(universe), new ArrayList<>());
    }

    /** Copy-constructor for ListVS
     * @param old The ListVS to copy
     */
    public ListVS(ListVS<T> old) {
        this(new PrimitiveVS<>(old.size), new ArrayList<>(old.items));
    }

    /** Is the list empty?
     * @return Whether the list is empty or not
     */
    public boolean isEmpty() {
        return isEmptyVS() || IntegerVS.maxValue(size) <= 0;
    }

    /**
     * Returns the size of the list value summary
     * @return Integer VS representing size under different guards
     */
    public PrimitiveVS<Integer> size() { return size; }

    /**
     * Is the value summary empty with no values in it
     */
    @Override
    public boolean isEmptyVS() {
        return size.isEmptyVS();
    }

    /**
     * Restrict the universe of the list value summary
     * @param guard The guard to conjoin to the current value summary's universe
     * @return Restricted value summary
     */
    @Override
    public ListVS<T> restrict(Guard guard) {
        final PrimitiveVS<Integer> newSize = size.restrict(guard);
        final List<T> newItems = new ArrayList<>();

        Integer max = IntegerVS.maxValue(newSize);
        if (max != null) {
            for (int i = 0; i < max; i++) {
                T newItem = this.items.get(i).restrict(guard);
                //assert (newItem.getUniverse().implies(newSize.getUniverse()).isConstTrue());
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
    // TODO: cleanup later
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
                    //assert i == itemsToMergeByIndex.size();
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
        // check if size is empty
        if (size.isEmptyVS()) {
            if (cmp.isEmptyVS()) {
                return BooleanVS.trueUnderGuard(pc);
            } else {
                return BooleanVS.trueUnderGuard(Guard.constFalse());
            }
        }

        Guard equalCond = Guard.constFalse();
        for (GuardedValue<Integer> size : this.size.getGuardedValues()) {
            if (cmp.size.hasValue(size.getValue())) {
                Guard listEqual = IntStream.range(0, size.getValue())
                        .mapToObj((i) -> this.items.get(i).symbolicEquals(cmp.items.get(i), pc).getGuardFor(Boolean.TRUE))
                        .reduce(Guard::and)
                        .orElse(Guard.constTrue());
                equalCond = equalCond.or(listEqual);
            }
        }
        return BooleanVS.trueUnderGuard(pc.and(equalCond));
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
        //assert(Checks.includedIn(item.getUniverse(), getUniverse()));
        PrimitiveVS<Integer> newSize = size.updateUnderGuard(item.getUniverse(), IntegerVS.add(size, 1));
        final List<T> newItems = new ArrayList<>(this.items);

        for (GuardedValue<Integer> possibleSize : this.size.restrict(item.getUniverse()).getGuardedValues()) {
            final int sizeValue = possibleSize.getValue();
            final T guardedItemToAdd = item.restrict(possibleSize.getGuard());

            if (sizeValue == newItems.size()) {
                newItems.add(guardedItemToAdd);
            } else {
                newItems.set(sizeValue, newItems.get(sizeValue).updateUnderGuard(possibleSize.getGuard(), guardedItemToAdd));
            }
        }
        //assert(Checks.sameUniverse(this.getUniverse(), newListVS.getUniverse()));
        return new ListVS<>(newSize, newItems);
    }

    /** Is index in range?
     * @param indexSummary The index to check
     */
    public PrimitiveVS<Boolean> inRange(PrimitiveVS<Integer> indexSummary) {
        return BooleanVS.and(IntegerVS.lessThan(indexSummary, size),
                IntegerVS.lessThan(-1, indexSummary));
    }

    /** Is an index in range?
     * @param index The index to check
     */
    public PrimitiveVS<Boolean> inRange(int index) {
        return BooleanVS.and(IntegerVS.lessThan(index, size), -1 < index);
    }

    /** Get an item from the ListVS
     * @param indexSummary The index to take from the ListVS. Should be possible under a subset of the ListVS's conditions.
     */
    public T get(PrimitiveVS<Integer> indexSummary) {
        //assert(Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        //assert(!indexSummary.isEmptyVS());
        return this.restrict(indexSummary.getUniverse()).getHelper(indexSummary);
    }

    public T getHelper(PrimitiveVS<Integer> indexSummary) {
        //(Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuardFor(false).isFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        T merger = null;
        List<T> toMerge = new ArrayList<>();
        // for each possible index value
        for (GuardedValue<Integer> index : indexSummary.getGuardedValues()) {
            T item = items.get(index.getValue()).restrict(index.getGuard());
            if (merger == null)
                merger = item;
            else
                toMerge.add(item);
        }

        return merger != null ? merger.merge(toMerge) : null;
    }

    /** Set an item in the ListVS
     * @param indexSummary The index to set in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @param itemToSet The item to put in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @return The result of setting the ListVS
     */
    public ListVS<T> set(PrimitiveVS<Integer> indexSummary, T itemToSet) {
        // if (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()))
        //    setHelper(indexSummary, itemToSet);
        //assert (Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        ListVS<T> guarded = this.restrict(indexSummary.getUniverse());
        if (guarded.getUniverse().isFalse()) return this;
        return updateUnderGuard(indexSummary.getUniverse(), guarded.setHelper(indexSummary, itemToSet));
    }

    /** Set an item in the ListVS
     * @param indexSummary The index to set in the ListVS. Should be possible under the same conditions as the ListVS.
     * @param itemToSet The item to put in the ListVS. Should be possible under the same conditions as the ListVS.
     * @return The result of setting the ListVS
     */
    private ListVS<T> setHelper(PrimitiveVS<Integer> indexSummary, T itemToSet) {
        /*
        assert(Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        assert(Checks.sameUniverse(itemToSet.getUniverse(), getUniverse()));
         */

        final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        ListVS<T> merger = null;
        List<ListVS<T>> toMerge = new ArrayList<>();
        // for each possible index value
        for (GuardedValue<Integer> index : indexSummary.getGuardedValues()) {
            final List<T> newItems = new ArrayList<>(items);
            // the original item is updated when this is the index (i.e., index.guard holds)
            final T newEntry = newItems.get(index.value).update(index.guard, itemToSet);
            newItems.set(index.value, newEntry);
            ListVS<T> newList = new ListVS<>(size, newItems).guard(index.guard);
            if (merger == null)
                merger = newList;
            else
                toMerge.add(newList);
        }

        return merger.merge(toMerge);
    }

    /** Check whether the ListVS contains an element
     * @param element The element to check for. Should be possible under a subset of the ListVS's conditions.
     * @return Whether or not the ListVS contains an element
     */
    public PrimitiveVS<Boolean> contains(T element) {
        return BoolUtils.fromTrueGuard(indexOf(element).getUniverse()).guard(this.getUniverse());
    }

    /** Insert an item in the ListVS.
     * @param indexSummary The index to insert at in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @param itemToInsert The item to put in the ListVS. Should be possible under the same subset of the ListVS's conditions.
     * @return The result of inserting into the ListVS
     */
    public ListVS<T> insert(PrimitiveVS<Integer> indexSummary, T itemToInsert) {
        /*
        assert(Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        assert(Checks.includedIn(itemToInsert.getUniverse(), getUniverse()));
        assert(Checks.sameUniverse(itemToInsert.getUniverse(), indexSummary.getUniverse()));
         */

        if (indexSummary.getUniverse().isConstFalse()) return this;

        Guard addUniverse = IntUtils.equalTo(indexSummary, size()).getGuard(true);
        if (!addUniverse.isConstFalse()) {
            return this.add(itemToInsert.guard(addUniverse))
                    .insert(indexSummary.guard(addUniverse.not()), itemToInsert.guard(addUniverse.not()));
        }

        final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        // 1. add a new entry (we'll re-add the last entry)
        ListVS<T> newList = new ListVS<>(this);
        newList = newList.add(newList.get(IntUtils.subtract(size, 1)).guard(indexSummary.getUniverse()));

        // 2. setting at the insertion index
        PrimitiveVS<Integer> current = indexSummary;
        T prev = newList.get(current);
        newList = newList.set(indexSummary, itemToInsert);
        current = IntUtils.add(current, 1);

        // 3. setting everything after insertion index to be the previous element
        while (BoolUtils.isEverTrue(IntUtils.lessThan(current, size))) {
            Guard guard = BoolUtils.trueCond(IntUtils.lessThan(current, size));
            T old = this.guard(guard).get(current.guard(guard));
            newList = newList.set(current.guard(guard), prev.guard(guard));
            prev = old;
            current = IntUtils.add(current, 1);
        }

        return newList;
    }

    /** Remove an item from the ListVS.
     * @param indexSummary The index to remove from in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @return The result of removing from the ListVS
     */
    public ListVS<T> removeAt(PrimitiveVS<Integer> indexSummary) {
        //assert (Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        ListVS<T> guarded = this.guard(indexSummary.getUniverse());
        ListVS<T> removed = guarded.removeAtHelper(indexSummary);
        return update(indexSummary.getUniverse(), removed);
    }

    /** Remove an item from the ListVS.
     * @param indexSummary The index to remove from in the ListVS. Should be possible under the same conditions as the ListVS.
     * @return The result of removing from the ListVS
     */
    private ListVS<T> removeAtHelper(PrimitiveVS<Integer> indexSummary) {
        //assert (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        final PrimitiveVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        // new size
        PrimitiveVS<Integer> newSize = IntUtils.subtract(size, 1);

        /** Optimize case where the index can only take on one value */
        if (indexSummary.getValues().size() == 1) {
            int idx = indexSummary.getValues().iterator().next();
            List<T> newItems = new ArrayList<T>(items);
            newItems.remove(idx);
            return new ListVS<>(newSize, newItems);
        }

        ListVS<T> newList = new ListVS<>(newSize, items.subList(0, items.size() - 1));
        PrimitiveVS<Integer> current = indexSummary;

        // Setting everything after removal index to be the next element
        while (BoolUtils.isEverTrue(IntUtils.lessThan(IntUtils.add(current, 1), size))) {
            //ScheduleLogger.log("removeAt while " + current);
            Guard thisCond = BoolUtils.trueCond(IntUtils.lessThan(IntUtils.add(current, 1), size));
            current = current.guard(thisCond);
            T next = this.get(IntUtils.add(current, 1));
            newList = newList.set(current, next);
            current = IntUtils.add(current, 1);
        }

        return newList;
    }

    /** Get the index of an element in the ListVS
     * @param element The element to check for. Should be possible under a subset of the ListVS's conditions.
     * @return The index of the element under the universe in which it is present
     */
    public PrimitiveVS<Integer> indexOf(T element) {
        //assert(Checks.includedIn(element.getUniverse(), getUniverse()));
        if (element.getUniverse().isConstFalse()) {
            return new PrimitiveVS<>();
        }
        //System.out.println(this.guard(element.getUniverse()));
        PrimitiveVS<Integer> i = new PrimitiveVS<>(0).guard(element.getUniverse());

        PrimitiveVS<Integer> index = new PrimitiveVS<>();
        ListVS<T> guarded = this.guard(element.getUniverse());

        while (BoolUtils.isEverTrue(IntUtils.lessThan(i, guarded.size)))  {
            Guard cond = BoolUtils.trueCond(IntUtils.lessThan(i, guarded.size));
            Guard contains = BoolUtils.trueCond(element.guard(cond).symbolicEquals(guarded.get(i.guard(cond)), cond));
            index = index.merge(i.guard(contains));
            i = IntUtils.add(i, 1);
        }

        return index;
    }

    /** Get the universe under which the data structure is nonempty
     * @return The universe under which the data structure is nonempty */
    public Guard getNonEmptyUniverse() {
        if (size.getGuardedValues().size() > 1) {
            /*
            ScheduleLogger.log("universe: " + getUniverse());
            for (GuardedValue<Integer> s : size.getGuardedValues()) {
                ScheduleLogger.log(s.value + " with guard: " + s.guard);
            }

             */
            if(getUniverse().and(size.getGuard(0).not()).isConstFalse()) {
                ScheduleLogger.log("ERROR!");
                throw new RuntimeException();
            }
        }
        return getUniverse().and(size.getGuard(0).not());
    }

    @Override
    public String toString() {
        String out = "";
        for (GuardedValue<Integer> guardedValue : size.getGuardedValues()) {
            out += "{";
            for (int i = 0; i < guardedValue.value; i++) {
                out += this.items.get(i).guard(guardedValue.guard);
                if (i < guardedValue.value - 1) {
                    out += "  ,   ";
                }
            }
            out += "}" + System.lineSeparator();
        }
        return out;
    }
}
