package psymbolic.runtime.machine.buffer;

import psymbolic.valuesummary.ListVS;
import psymbolic.valuesummary.PrimitiveVS;
import psymbolic.valuesummary.ValueSummary;
import psymbolic.valuesummary.Guard;

import java.util.function.Function;

/**
 * Represents a event-queue implementation using value summaries
 * @param <T>
 */
public class SymbolicQueue<T extends ValueSummary<T>> {

    // elements in the queue
    private ListVS<T> elements;
    private T peek = null;

    public SymbolicQueue() {
        this.elements = new ListVS<>(Guard.constTrue());
        assert(elements.getUniverse().isTrue());
    }

    public PrimitiveVS<Integer> size() { return elements.size(); }

    public PrimitiveVS<Integer> size(Guard pc) { return elements.restrict(pc).size(); }

    public void enqueue(T entry) {
        elements = elements.add(entry);
    }

    public boolean isEmpty() {
        return elements.isEmpty();
    }

    public Guard isEnabledUnderGuard() {
        return elements.getNonEmptyUniverse();
    }

    /** Get the condition under which the first queue entry satisfies the provided predicate
     * @param pred The filtering predicate
     * @return The condition under which the first queue entry obeys pred
     */
    public PrimitiveVS<Boolean> satisfiesPredUnderGuard(Function<T, PrimitiveVS<Boolean>> pred) {
        Guard cond = isEnabledUnderGuard();
        assert(!cond.isFalse());
        T top = peek(cond);
        return pred.apply(top).restrict(top.getUniverse());
    }

    public T dequeueEntry(Guard pc) {
        return peekOrDequeueHelper(pc, true);
    }

    public T peek(Guard pc) {
        return peekOrDequeueHelper(pc, false);
    }

    private T peekOrDequeueHelper(Guard pc, boolean dequeue) {
        boolean updatePeek = peek == null || !pc.implies(peek.getUniverse()).isTrue();
        if (!dequeue && !updatePeek) {
            return peek.restrict(pc);
        }
        assert(elements.getUniverse().isTrue());
        ListVS<T> filtered = elements.restrict(pc);
        if (updatePeek) {
            peek = filtered.get(new PrimitiveVS<>(0).restrict(pc));
        }
        T ret = peek.restrict(pc);
        if (dequeue) {
            elements = elements.removeAt(new PrimitiveVS<>(0).restrict(pc));
            peek = null;
        }
        assert(!pc.isFalse());
        return ret;
    }

    @Override
    public String toString() {
        return String.format("EventQueue{elements=%s}", elements);
    }
}
