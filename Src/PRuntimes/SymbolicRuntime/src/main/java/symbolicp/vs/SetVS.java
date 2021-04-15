package symbolicp.vs;

import symbolicp.bdd.Bdd;
import symbolicp.util.Checks;

import java.util.*;
import java.util.stream.Collectors;

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

    public SetVS(Bdd universe) {
        this.elements = new ListVS<>(universe);
    }

    public PrimVS<Integer> size() {
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
    public SetVS<T> guard(Bdd guard) {
        return new SetVS<>(new ListVS<>(elements.guard(guard)));
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
    public SetVS<T> update(Bdd guard, SetVS<T> update) {
        return this.guard(guard.not()).merge(Collections.singletonList(update.guard(guard)));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(SetVS<T> cmp, Bdd pc) {
        return this.elements.symbolicEquals(cmp.elements, pc);
    }

    @Override
    public Bdd getUniverse() {
        return elements.getUniverse();
    }

    /** Check whether the SetVS contains an element
     *
     * @param itemSummary The element to check for. Should be possible under a subset of the SetVS's conditions.
     * @return Whether or not the SetVS contains an element
     */
    public PrimVS<Boolean> contains(T itemSummary) {
        return elements.contains(itemSummary);
    }

    /** Get the universe under which the data structure is nonempty
     *
     * @return The universe under which the data structure is nonempty */
    public Bdd getNonEmptyUniverse() { return elements.getNonEmptyUniverse(); }

    /** Add an item to the SetVS.
     *
     * @param itemSummary The element to add.
     * @return The SetVS with the element added
     */
    public SetVS<T> add(T itemSummary) {
        // Not already included?
        Bdd absent = contains(itemSummary.guard(getUniverse())).getGuard(false);

        ListVS<T> newElements = elements.update(absent, elements.add(itemSummary));

        return new SetVS<>(newElements);
    }

    /** Remove an item from the SetVS.
     *
     * @param itemSummary The element to remove. Should be possible under a subset of the SetVS's conditions.
     * @return The SetVS with the element removed.
     */
    public SetVS<T> remove(T itemSummary) {
        ListVS<T> newElements = elements.removeAt(elements.indexOf(itemSummary));
        return new SetVS<>(newElements);
    }
}
