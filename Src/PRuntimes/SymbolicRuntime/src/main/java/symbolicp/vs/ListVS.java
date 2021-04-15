package symbolicp.vs;

import symbolicp.bdd.Bdd;
import symbolicp.runtime.ScheduleLogger;
import symbolicp.util.Checks;

import java.util.*;
import java.util.stream.IntStream;

/** Class for list value summaries */
public class ListVS<T extends ValueSummary<T>> implements ValueSummary<ListVS<T>> {
    /** The size of the list */
    private final PrimVS<Integer> size;
    /** The contents of the list */
    private final List<T> items;

    private ListVS(PrimVS<Integer> size, List<T> items) {
        this.size = size;
        this.items = items;
    }

    /** Make a new ListVS with the specified universe
     * @param universe The universe for the new ListVS
     */
    public ListVS(Bdd universe) {
        this(new PrimVS<>(0).guard(universe), new ArrayList<>());
    }

    /** Copy-constructor for ListVS
     * @param old The ListVS to copy
     */
    public ListVS(ListVS<T> old) {
        this(new PrimVS<>(old.size), new ArrayList<>(old.items));
    }

    /** Is the list empty?
     * @return Whether the list is empty or not
     */
    public boolean isEmpty() {
        return isEmptyVS() || IntUtils.maxValue(size) <= 0;
    }

    public PrimVS<Integer> size() { return size; }

    @Override
    public boolean isEmptyVS() {
        return size.isEmptyVS();
    }

    @Override
    public ListVS<T> guard(Bdd guard) {
        final PrimVS<Integer> newSize = size.guard(guard);
        final List<T> newItems = new ArrayList<>();

        Integer max = IntUtils.maxValue(newSize);
        if (max != null) {
            for (int i = 0; i < max; i++) {
                T newItem = this.items.get(i).guard(guard);
                //assert (newItem.getUniverse().implies(newSize.getUniverse()).isConstTrue());
                newItems.add(newItem);
            }
        }
        return new ListVS<>(newSize, newItems);
    }

    @Override
    public ListVS<T> update(Bdd guard, ListVS<T> update) {
        ListVS<T> res = this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
        return res;
    }

    @Override
    public ListVS<T> merge(Iterable<ListVS<T>> summaries) {
        final List<PrimVS<Integer>> sizesToMerge = new ArrayList<>();
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

        final PrimVS<Integer> mergedSize = size.merge(sizesToMerge);

        final List<T> mergedItems = new ArrayList<>();

        for (List<T> itemsToMerge : itemsToMergeByIndex) {
            // TODO: cleanup later
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
    public PrimVS<Boolean> symbolicEquals(ListVS<T> cmp, Bdd pc) {
        if (size.isEmptyVS()) {
            if (cmp.isEmptyVS()) {
                return BoolUtils.fromTrueGuard(pc);
            } else {
                return BoolUtils.fromTrueGuard(Bdd.constFalse());
            }
        }

        Bdd equalCond = Bdd.constFalse();
        for (GuardedValue<Integer> size : this.size.getGuardedValues()) {
            if (cmp.size.hasValue(size.value)) {
                Bdd listEqual = IntStream.range(0, size.value)
                        .mapToObj((i) -> this.items.get(i).symbolicEquals(cmp.items.get(i), pc).getGuard(Boolean.TRUE))
                        .reduce(Bdd::and)
                        .orElse(Bdd.constTrue());
                equalCond = equalCond.or(listEqual);
            }
        }
        return BoolUtils.fromTrueGuard(pc.and(equalCond));
    }

    @Override
    public Bdd getUniverse() {
        return size.getUniverse();
    }

    /** Add an item to the ListVS
     *
     * @param item The Item to add to the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @return The updated ListVS
     */
    public ListVS<T> add(T item) {
        //assert(Checks.includedIn(item.getUniverse(), getUniverse()));
        PrimVS<Integer> newSize = size.update(item.getUniverse(), IntUtils.add(size, 1));
        final List<T> newItems = new ArrayList<>(this.items);

        for (GuardedValue<Integer> possibleSize : this.size.guard(item.getUniverse()).getGuardedValues()) {
            final int sizeValue = possibleSize.value;
            final T guardedItemToAdd = item.guard(possibleSize.guard);

            if (sizeValue == newItems.size()) {
                newItems.add(guardedItemToAdd);
            } else {
                newItems.set(sizeValue, newItems.get(sizeValue).update(possibleSize.guard, guardedItemToAdd));
            }
        }

        ListVS<T> newListVS = new ListVS<>(newSize, newItems);
        //assert(Checks.sameUniverse(this.getUniverse(), newListVS.getUniverse()));
        return newListVS;
    }

    /** Is an index in range?
     * @param indexSummary The index to check
     */
    public PrimVS<Boolean> inRange(PrimVS<Integer> indexSummary) {
        return BoolUtils.and(IntUtils.lessThan(indexSummary, size),
                IntUtils.lessThan(-1, indexSummary));
    }

    /** Is an index in range?
     * @param index The index to check
     */
    public PrimVS<Boolean> inRange(int index) {
        return BoolUtils.and(IntUtils.lessThan(index, size), -1 < index);
    }

    /** Get an item from the ListVS
     * @param indexSummary The index to take from the ListVS. Should be possible under a subset of the ListVS's conditions.
     */
    public T get(PrimVS<Integer> indexSummary) {
        //assert(Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        //assert(!indexSummary.isEmptyVS());
        return this.guard(indexSummary.getUniverse()).getHelper(indexSummary);
    }

    public T getHelper(PrimVS<Integer> indexSummary) {
        //(Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        final PrimVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            /*
            ScheduleLogger.log("index summ values: " + indexSummary);
            ScheduleLogger.log("size values: " + size.guard());

             */
            throw new IndexOutOfBoundsException();
        }

        T merger = null;
        List<T> toMerge = new ArrayList<>();
        //System.out.println("size: " + indexSummary.getGuardedValues().size());
        // for each possible index value
        for (GuardedValue<Integer> index : indexSummary.getGuardedValues()) {
            T item = items.get(index.value).guard(index.guard);
            if (merger == null)
                merger = item;
            else
                toMerge.add(item);
        }

        return merger.merge(toMerge);
    }

    /** Set an item in the ListVS
     * @param indexSummary The index to set in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @param itemToSet The item to put in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @return The result of setting the ListVS
     */
    public ListVS<T> set(PrimVS<Integer> indexSummary, T itemToSet) {
        //if (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()))
        //    setHelper(indexSummary, itemToSet);
        //assert (Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        ListVS<T> guarded = this.guard(indexSummary.getUniverse());
        if (guarded.getUniverse().isConstFalse()) return this;
        return update(indexSummary.getUniverse(), guarded.setHelper(indexSummary, itemToSet));
    }

    /** Set an item in the ListVS
     * @param indexSummary The index to set in the ListVS. Should be possible under the same conditions as the ListVS.
     * @param itemToSet The item to put in the ListVS. Should be possible under the same conditions as the ListVS.
     * @return The result of setting the ListVS
     */
    private ListVS<T> setHelper(PrimVS<Integer> indexSummary, T itemToSet) {
        /*
        assert(Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        assert(Checks.sameUniverse(itemToSet.getUniverse(), getUniverse()));
         */

        final PrimVS<Boolean> inRange = inRange(indexSummary);
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
    public PrimVS<Boolean> contains(T element) {
        return BoolUtils.fromTrueGuard(indexOf(element).getUniverse()).guard(this.getUniverse());
    }

    /** Insert an item in the ListVS.
     * @param indexSummary The index to insert at in the ListVS. Should be possible under a subset of the ListVS's conditions.
     * @param itemToInsert The item to put in the ListVS. Should be possible under the same subset of the ListVS's conditions.
     * @return The result of inserting into the ListVS
     */
    public ListVS<T> insert(PrimVS<Integer> indexSummary, T itemToInsert) {
        /*
        assert(Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        assert(Checks.includedIn(itemToInsert.getUniverse(), getUniverse()));
        assert(Checks.sameUniverse(itemToInsert.getUniverse(), indexSummary.getUniverse()));
         */

        if (indexSummary.getUniverse().isConstFalse()) return this;

        Bdd addUniverse = IntUtils.equalTo(indexSummary, size()).getGuard(true);
        if (!addUniverse.isConstFalse()) {
            return this.add(itemToInsert.guard(addUniverse))
                    .insert(indexSummary.guard(addUniverse.not()), itemToInsert.guard(addUniverse.not()));
        }

        final PrimVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        // 1. add a new entry (we'll re-add the last entry)
        ListVS<T> newList = new ListVS<>(this);
        newList = newList.add(newList.get(IntUtils.subtract(size, 1)).guard(indexSummary.getUniverse()));

        // 2. setting at the insertion index
        PrimVS<Integer> current = indexSummary;
        T prev = newList.get(current);
        newList = newList.set(indexSummary, itemToInsert);
        current = IntUtils.add(current, 1);

        // 3. setting everything after insertion index to be the previous element
        while (BoolUtils.isEverTrue(IntUtils.lessThan(current, size))) {
            Bdd guard = BoolUtils.trueCond(IntUtils.lessThan(current, size));
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
    public ListVS<T> removeAt(PrimVS<Integer> indexSummary) {
        //assert (Checks.includedIn(indexSummary.getUniverse(), getUniverse()));
        ListVS<T> guarded = this.guard(indexSummary.getUniverse());
        ListVS<T> removed = guarded.removeAtHelper(indexSummary);
        return update(indexSummary.getUniverse(), removed);
    }

    /** Remove an item from the ListVS.
     * @param indexSummary The index to remove from in the ListVS. Should be possible under the same conditions as the ListVS.
     * @return The result of removing from the ListVS
     */
    private ListVS<T> removeAtHelper(PrimVS<Integer> indexSummary) {
        //assert (Checks.sameUniverse(indexSummary.getUniverse(), getUniverse()));
        final PrimVS<Boolean> inRange = inRange(indexSummary);
        // make sure it is always in range
        if (!inRange.getGuard(false).isConstFalse()) {
            // there is a possibility that the index is out-of-bounds
            throw new IndexOutOfBoundsException();
        }

        // new size
        PrimVS<Integer> newSize = IntUtils.subtract(size, 1);

        /** Optimize case where the index can only take on one value */
        if (indexSummary.getValues().size() == 1) {
            int idx = indexSummary.getValues().iterator().next();
            List<T> newItems = new ArrayList<T>(items);
            newItems.remove(idx);
            return new ListVS<>(newSize, newItems);
        }

        ListVS<T> newList = new ListVS<>(newSize, items.subList(0, items.size() - 1));
        PrimVS<Integer> current = indexSummary;

        // Setting everything after removal index to be the next element
        while (BoolUtils.isEverTrue(IntUtils.lessThan(IntUtils.add(current, 1), size))) {
            ScheduleLogger.log("removeAt while " + current);
            Bdd thisCond = BoolUtils.trueCond(IntUtils.lessThan(IntUtils.add(current, 1), size));
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
    public PrimVS<Integer> indexOf(T element) {
        //assert(Checks.includedIn(element.getUniverse(), getUniverse()));
        if (element.getUniverse().isConstFalse()) {
            return new PrimVS<>();
        }
        //System.out.println(this.guard(element.getUniverse()));
        PrimVS<Integer> i = new PrimVS<>(0).guard(element.getUniverse());

        PrimVS<Integer> index = new PrimVS<>();
        ListVS<T> guarded = this.guard(element.getUniverse());

        while (BoolUtils.isEverTrue(IntUtils.lessThan(i, guarded.size)))  {
            Bdd cond = BoolUtils.trueCond(IntUtils.lessThan(i, guarded.size));
            Bdd contains = BoolUtils.trueCond(element.guard(cond).symbolicEquals(guarded.get(i.guard(cond)), cond));
            index = index.merge(i.guard(contains));
            i = IntUtils.add(i, 1);
        }

        return index;
    }

    /** Get the universe under which the data structure is nonempty
     * @return The universe under which the data structure is nonempty */
    public Bdd getNonEmptyUniverse() {
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
