package psymbolic.valuesummary;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/** Class for set value summaries */
public class SetVS<T extends ValueSummary<T>> implements ValueSummary<SetVS<T>> {

    /** The underlying set */
    private final ListVS<T> elements;

    /** Get all the different possible guarded values */
    public ListVS<T> getElements() {
        return elements;
    }

    public SetVS(ListVS<T> elements) {
        this.elements = elements;
    }

    public SetVS(Guard universe) {
        this.elements = new ListVS<>(universe);
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
        if(guard.equals(getUniverse()))
            return new SetVS<T>(new ListVS<>(elements));

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
    public SetVS<T> combineVals(SetVS<T> other) {
        return new SetVS<>(elements.combineVals(other.elements));
    }

    @Override
    public SetVS<T> updateUnderGuard(Guard guard, SetVS<T> update) {
        return this.restrict(guard.not()).merge(Collections.singletonList(update.restrict(guard)));//.combineVals(this);
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(SetVS<T> cmp, Guard pc) {
        return this.elements.symbolicEquals(cmp.elements, pc);
    }

    @Override
    public Guard getUniverse() {
        return elements.getUniverse();
    }

    /** Check whether the SetVS contains an element
     *
     * @param itemSummary The element to check for. Should be possible under a subset of the SetVS's conditions.
     * @return Whether or not the SetVS contains an element
     */
    public PrimitiveVS<Boolean> contains(T itemSummary) {
        return elements.contains(itemSummary);
    }

    /** Get the universe under which the data structure is nonempty
     *
     * @return The universe under which the data structure is nonempty */
    public Guard getNonEmptyUniverse() { return elements.getNonEmptyUniverse(); }

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
     * @param itemSummary The element to remove. Should be possible under a subset of the SetVS's conditions.
     * @return The SetVS with the element removed.
     */
    public SetVS<T> remove(T itemSummary) {
        PrimitiveVS<Integer> idx = elements.indexOf(itemSummary);
        idx = idx.restrict(elements.inRange(idx).getGuardFor(true));
        if (idx.isEmptyVS()) return this;
        ListVS<T> newElements = elements.removeAt(idx);
        return new SetVS<>(newElements);
    }
}
