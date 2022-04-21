package psymbolic.runtime.machine.buffer;

import lombok.Getter;
import psymbolic.runtime.NondetUtil;
import psymbolic.runtime.*;
import psymbolic.runtime.machine.*;
import psymbolic.valuesummary.*;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.HashMap;
import java.util.function.Function;

/**
 * Represents a value summary based symbolic implementation of a Bag
 * @param <T> Type of elements allowed in the Bag
 */
public class SymbolicBag<T extends ValueSummary<T>> implements Serializable {

    // items in the bag
    @Getter
    protected ListVS<T> elements;

    public SymbolicBag() {
        this.elements = new ListVS<>(Guard.constTrue());
        assert(elements.getUniverse().isTrue());
    }

    public PrimitiveVS<Integer> size() { return elements.size(); }

    public void add(T entry) {
        elements = elements.add(entry);
    }

    public boolean isEmpty() {
        return elements.isEmpty();
    }

    public Guard isEnabledUnderGuard() {
        return elements.getNonEmptyUniverse();
    }

    /** Get all the conditions under which the elements in the bag satisfy the provided predicate
     * @param pred The filtering predicate
     * @return All the condition under which the elements in the bad obeys the supplied pred
     */
    public PrimitiveVS<Boolean> satisfiesPredUnderGuardsAll(Function<T, PrimitiveVS<Boolean>> pred) {
        Guard cond = elements.getNonEmptyUniverse();
        ListVS<T> elts = elements.restrict(cond);
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(cond);
        PrimitiveVS<Boolean> enabledCond = new PrimitiveVS<>(false);
        while (BooleanVS.isEverTrue(IntegerVS.lessThan(idx, elts.size()))) {
            Guard iterCond = IntegerVS.lessThan(idx, elts.size()).getGuardFor(true);
            PrimitiveVS<Boolean> res = pred.apply(elts.get(idx.restrict(iterCond)));
            enabledCond = BooleanVS.or(enabledCond, res);
            idx = IntegerVS.add(idx, 1);
        }
        return enabledCond;
    }

    public T peek(Guard pc) {
        assert (elements.getUniverse().isTrue());
        ListVS<T> filtered = elements.restrict(pc);
        PrimitiveVS<Integer> size = filtered.size();
        List<PrimitiveVS> choices = new ArrayList<>();
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(pc);
        while(BooleanVS.isEverTrue(IntegerVS.lessThan(idx, size))) {
            Guard cond = IntegerVS.lessThan(idx, size).getGuardFor(true);
            choices.add(idx.restrict(cond));
            idx = IntegerVS.add(idx, 1);
        }
        PrimitiveVS<Integer> index = (PrimitiveVS<Integer>) NondetUtil.getNondetChoice(choices);
        return filtered.restrict(index.getUniverse()).get(index);
    }
    
    public T remove(Guard pc) {
        assert (elements.getUniverse().isTrue());
        ListVS<T> filtered = elements.restrict(pc);
        PrimitiveVS<Integer> size = filtered.size();
        List<PrimitiveVS> choices = new ArrayList<>();
        PrimitiveVS<Integer> idx = new PrimitiveVS<>(0).restrict(pc);
        while(BooleanVS.isEverTrue(IntegerVS.lessThan(idx, size))) {
            Guard cond = IntegerVS.lessThan(idx, size).getGuardFor(true);
            choices.add(idx.restrict(cond));
            idx = IntegerVS.add(idx, 1);
        }
        PrimitiveVS<Integer> index = (PrimitiveVS<Integer>) NondetUtil.getNondetChoice(choices);
        T element = filtered.restrict(index.getUniverse()).get(index);
        elements = elements.removeAt(index);
        return element;
    }

    @Override
    public String toString() {
        return "EventBag {" + "elements=" + elements + '}';
    }
}
