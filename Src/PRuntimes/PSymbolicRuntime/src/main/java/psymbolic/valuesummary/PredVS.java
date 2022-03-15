package psymbolic.valuesummary;

import java.util.*;

public class PredVS<T> implements ValueSummary<PredVS<T>> {

    private final PrimitiveVS<Set<T>> primVS;

    public PredVS() {
        primVS = new PrimitiveVS<>(new HashSet<>());
    }

    public PredVS(Map<Set<T>, Guard> map) {
        primVS = new PrimitiveVS<>(map);
    }

    public PredVS(PrimitiveVS<Set<T>> impl) {
        primVS = new PrimitiveVS<>(impl);
    }

    public PredVS(SetVS<PredVS<T>> set, Guard guard) {
        primVS = new PredVS<>(set).primVS.restrict(guard);
    }

    public PredVS(SetVS<PredVS<T>> set) {
        this(set.getElements());
    }

    public PredVS(ListVS<PredVS<T>> list, Guard guard) {
        primVS = new PredVS<>(list).primVS.restrict(guard);
    }

    public PredVS(ListVS<PredVS<T>> list) {
        int idx = 0;
        PredVS<T> tmp = new PredVS<>();
        Guard inRange = list.inRange(idx).getGuardFor(true);
        while (!inRange.isFalse()) {
            PredVS<T> elt = list.get(new PrimitiveVS<>(idx).restrict(inRange));
            tmp = tmp.updateUnderGuard(inRange, elt).combineVals(tmp);
            idx++;
            inRange = list.inRange(idx).getGuardFor(true);
        }
        primVS = tmp.primVS;
    }


    public PredVS(T element) {
        primVS = new PrimitiveVS<>(Collections.singleton(element));
    }

    public List<PrimitiveVS<T>> getValues() {
        List<PrimitiveVS<T>> res = new ArrayList<>();
        for (GuardedValue<Set<T>> gv : primVS.getGuardedValues()) {
            for (T elt : gv.getValue()) {
                res.add(new PrimitiveVS<>(elt).restrict(gv.getGuard()));
            }
        }
        return res;
    }

    @Override
    public boolean isEmptyVS() {
        return primVS.isEmptyVS();
    }

    @Override
    /** Add values from other to sets in this with the same guards */
    public PredVS<T> combineVals(PredVS<T> other) {
        List<GuardedValue<Set<T>>> currThis = primVS.getGuardedValues();
        for (GuardedValue<Set<T>> otherGV : other.primVS.getGuardedValues()) {
            List<GuardedValue<Set<T>>> newThis = new ArrayList<>();
            for (GuardedValue<Set<T>> thisGV : currThis) {
                if (!thisGV.getValue().containsAll(otherGV.getValue())) {
                    Guard conj = thisGV.getGuard().and(otherGV.getGuard());
                    if (!conj.isFalse()) {
                        // there's an overlap
                        Set<T> newSet = new HashSet<>(thisGV.getValue());
                        newSet.addAll(otherGV.getValue());
                        // add the overlap to the new set of guarded values
                        newThis.add(new GuardedValue<>(newSet, conj));
                        // add everything outside the overlap to the new set of guarded values
                        Guard excludeOther = thisGV.getGuard().and(otherGV.getGuard().not());
                        newThis.add(new GuardedValue<>(thisGV.getValue(), excludeOther));
                        continue;
                    }
                }
                // no overlap, so nothing to add--keep the current guarded value
                newThis.add(thisGV);
            }
            currThis = newThis;

        }

        Map<Set<T>, Guard> map = new HashMap<>();

        for (GuardedValue<Set<T>> gv : currThis) {
            map.merge(gv.getValue(), gv.getGuard(), Guard::or);
        }

        return new PredVS<>(map);
    }

    @Override
    public PredVS<T> restrict(Guard guard) {
        return new PredVS<>(primVS.restrict(guard));
    }

    @Override
    public PredVS<T> merge(Iterable<PredVS<T>> summaries) {
        List<PrimitiveVS<Set<T>>> primVSSummaries = new ArrayList<>();
        for (PredVS<T> summary : summaries) primVSSummaries.add(summary.primVS);
        return new PredVS<>(primVS.merge(primVSSummaries));
    }

    @Override
    public PredVS<T> merge(PredVS<T> summary) {
        return new PredVS<>(primVS.merge(summary.primVS));
    }

    @Override
    public PredVS<T> updateUnderGuard(Guard guard, PredVS<T> updateVal) {
        return this.restrict(guard.not()).merge(Collections.singletonList(updateVal.restrict(guard)));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(PredVS<T> cmp, Guard guard) {
        return this.primVS.symbolicEquals(cmp.primVS, guard);
    }

    @Override
    public Guard getUniverse() {
        return primVS.getUniverse();
    }

    @Override
    public String toString() {
        return primVS.toString();
    }
}
