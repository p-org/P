package psymbolic.runtime;

import psymbolic.valuesummary.ListVS;
import psymbolic.valuesummary.PrimVS;
import psymbolic.valuesummary.ValueSummary;
import psymbolic.valuesummary.bdd.Bdd;

import java.util.function.Function;

public class SymbolicQueue<T extends ValueSummary<T>> {

    private ListVS<T> entries;
    private T peek = null;

    public SymbolicQueue() {
        this.entries = new ListVS<>(Bdd.constTrue());
        assert(entries.getUniverse().isConstTrue());
    }

    public PrimVS<Integer> size() { return entries.size(); }

    public PrimVS<Integer> size(Bdd pc) { return entries.guard(pc).size(); }


    public void enqueueEntry(T entry) {
        entries = entries.add(entry);
    }

    public boolean isEmpty() {
        return entries.isEmpty();
    }

    public Bdd enabledCond() {
        return entries.getNonEmptyUniverse();
    }

    /** Get the condition under which the first queue entry obeys the provided predicate
     * @param pred The filtering predicate
     * @return The condition under which the first queue entry obeys pred
     */
    public PrimVS<Boolean> enabledCond(Function<T, PrimVS<Boolean>> pred) {
        Bdd cond = enabledCond();
        assert(!cond.isConstFalse());
        T top = peek(cond);
        return pred.apply(top).guard(top.getUniverse());
    }

    public T dequeueEntry(Bdd pc) {
        T res = peekOrDequeueHelper(pc, true);
        return res;
        //return peekOrDequeueHelper(pc, true);
    }

    public T peek(Bdd pc) {
        return peekOrDequeueHelper(pc, false);
    }

    private T peekOrDequeueHelper(Bdd pc, boolean dequeue) {
        boolean updatePeek = peek == null || !pc.implies(peek.getUniverse()).isConstTrue();
        if (!dequeue && !updatePeek) {
            return peek.guard(pc);
        }
        assert(entries.getUniverse().isConstTrue());
        ListVS<T> filtered = entries.guard(pc);
        if (updatePeek) {
            peek = filtered.get(new PrimVS<>(0).guard(pc));
        }
        T ret = peek.guard(pc);
        if (dequeue) {
            entries = entries.removeAt(new PrimVS<>(0).guard(pc));
            peek = null;
        }
        assert(!pc.isConstFalse());
        return ret;
    }

    @Override
    public String toString() {
        return "SymbolicQueue{" +
                "entries=" + entries +
                '}';
    }
}
