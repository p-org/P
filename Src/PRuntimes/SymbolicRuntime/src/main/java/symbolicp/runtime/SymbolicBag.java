package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.util.Checks;
import symbolicp.vs.*;

import java.util.ArrayList;
import java.util.List;
import java.util.function.Function;

public class SymbolicBag<T extends ValueSummary<T>> {

    private ListVS<T> entries;

    public SymbolicBag() {
        this.entries = new ListVS<>(Bdd.constTrue());
        assert(entries.getUniverse().isConstTrue());
    }

    public PrimVS<Integer> size() { return entries.size(); }

    public void add(T entry) {
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
        Bdd cond = entries.getNonEmptyUniverse();
        ListVS<T> elts = entries.guard(cond);
        PrimVS<Integer> idx = new PrimVS<>(0).guard(cond);
        PrimVS<Boolean> enabledCond = new PrimVS<>(false);
        while (BoolUtils.isEverTrue(IntUtils.lessThan(idx, elts.size()))) {
            Bdd iterCond = IntUtils.lessThan(idx, elts.size()).getGuard(true);
            PrimVS<Boolean> res = pred.apply(elts.get(idx.guard(iterCond)));
            enabledCond = BoolUtils.or(enabledCond, res);
            idx = IntUtils.add(idx, 1);
        }
        return enabledCond;
    }

    /** Get a condition under which a queue entry that obeys the provided predicate exists
     * @param pred The filtering predicate
     * @return The condition under which the first queue entry obeys pred
     */
    public PrimVS<Boolean> enabledCondOne(Function<T, PrimVS<Boolean>> pred) {
        Bdd cond = entries.getNonEmptyUniverse();
        ListVS<T> elts = entries.guard(cond);
        PrimVS<Integer> idx = new PrimVS<>(0).guard(cond);
        while (BoolUtils.isEverTrue(IntUtils.lessThan(idx, elts.size()))) {
            Bdd iterCond = IntUtils.lessThan(idx, elts.size()).getGuard(true);
            PrimVS<Boolean> res = pred.apply(elts.get(idx.guard(iterCond)));
            if (!res.getGuard(true).isConstFalse()) {
                return res;
            }
            idx = IntUtils.add(idx, 1);
        }
        return new PrimVS<>(false);
    }

    public T peek(Bdd pc) {
        assert (entries.getUniverse().isConstTrue());
        ListVS<T> filtered = entries.guard(pc);
        PrimVS<Integer> size = filtered.size();
        List<PrimVS> choices = new ArrayList<>();
        PrimVS<Integer> idx = new PrimVS<>(0).guard(pc);
        while(BoolUtils.isEverTrue(IntUtils.lessThan(idx, size))) {
            Bdd cond = IntUtils.lessThan(idx, size).getGuard(true);
            choices.add(idx.guard(cond));
            idx = IntUtils.add(idx, 1);
        }
        PrimVS<Integer> index = (PrimVS<Integer>) NondetUtil.getNondetChoice(choices);
        T element = filtered.guard(index.getUniverse()).get(index);
        return element;
    }

    public T remove(Bdd pc) {
        assert (entries.getUniverse().isConstTrue());
        ListVS<T> filtered = entries.guard(pc);
        PrimVS<Integer> size = filtered.size();
        List<PrimVS> choices = new ArrayList<>();
        PrimVS<Integer> idx = new PrimVS<>(0).guard(pc);
        while(BoolUtils.isEverTrue(IntUtils.lessThan(idx, size))) {
            Bdd cond = IntUtils.lessThan(idx, size).getGuard(true);
            choices.add(idx.guard(cond));
            idx = IntUtils.add(idx, 1);
        }
        PrimVS<Integer> index = (PrimVS<Integer>) NondetUtil.getNondetChoice(choices);
        T element = filtered.guard(index.getUniverse()).get(index);
        entries = entries.removeAt(index);
        return element;
    }

    @Override
    public String toString() {
        return "SymbolicQueue{" +
                "entries=" + entries +
                '}';
    }
}
