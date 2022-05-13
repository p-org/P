package psymbolic.valuesummary;

import psymbolic.valuesummary.Guard;

import java.util.ArrayList;
import java.util.List;

public class VectorClockVS implements ValueSummary<VectorClockVS> {

    private final ListVS<PrimitiveVS<Integer>> clock;

    public ListVS<PrimitiveVS<Integer>> asListVS() {
        return clock;
    }

    public VectorClockVS(Guard universe) {
        this.clock = new ListVS<>(universe);
    }

    /** Copy-constructor for VectorClockVS
     * @param old The VectorClockVS to copy
     */
    public VectorClockVS(VectorClockVS old) {
        this.clock = new ListVS<>(old.clock);
    }

    public VectorClockVS(ListVS<PrimitiveVS<Integer>> clock) {
        this.clock = clock;
    }

    /**
     * Copy the value summary
     *
     * @return A new cloned copy of the value summary
     */
    public VectorClockVS getCopy() {
        return new VectorClockVS(this);
    }

    public PrimitiveVS<Integer> size() {
        return clock.size();
    }

    public VectorClockVS extend (PrimitiveVS<Integer> size) {
        PrimitiveVS<Integer> currentSize = this.size();
        ListVS<PrimitiveVS<Integer>> extended = clock;
        PrimitiveVS<Boolean> lessThan = IntegerVS.lessThan(currentSize, size);
        while (lessThan.hasValue(true)) {
            Guard lessThanCond = lessThan.getGuardFor(true);
            extended = extended.add(new PrimitiveVS<>(0).restrict(lessThanCond));
            currentSize = extended.size();
            lessThan = IntegerVS.lessThan(currentSize, size);
        }
        return new VectorClockVS(extended);
    }

    public VectorClockVS increment(PrimitiveVS<Integer> idx, PrimitiveVS<Integer> amt) {
        ListVS<PrimitiveVS<Integer>> updatedClock = extend(IntegerVS.add(idx, 1)).clock;
        PrimitiveVS<Boolean> inRange = updatedClock.inRange(idx);
        Guard inRangeCond = inRange.getGuardFor(true);
        PrimitiveVS<Integer> updateValue = IntegerVS.add(updatedClock.get(idx.restrict(inRangeCond)), amt.restrict(inRangeCond));
        updatedClock = updatedClock.set(idx.restrict(inRangeCond), updateValue);
        return new VectorClockVS(updatedClock);
    }

    public VectorClockVS increment(PrimitiveVS<Integer> idx) {
        return increment(idx, new PrimitiveVS<>(1));
    }

    public VectorClockVS takeMax(PrimitiveVS<Integer> idx, PrimitiveVS<Integer> amt) {
        ListVS<PrimitiveVS<Integer>> updatedClock = extend(IntegerVS.add(idx, 1)).clock;
        PrimitiveVS<Boolean> inRange = updatedClock.inRange(idx);
        Guard inRangeCond = inRange.getGuardFor(true);
        PrimitiveVS<Integer> cmpResult = IntegerVS.compare(updatedClock.get(idx.restrict(inRangeCond)), amt.restrict(inRangeCond));
        Guard updateCond = IntegerVS.lessThan(cmpResult, 0).getGuardFor(true);
        updatedClock = updatedClock.set(idx.restrict(inRangeCond).restrict(updateCond), amt);
        return new VectorClockVS(updatedClock);
    }

    public VectorClockVS add(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS sum = new VectorClockVS(this);
        PrimitiveVS<Integer> size = vc.size();
        PrimitiveVS<Boolean> lessThan = IntegerVS.lessThan(idx, size);
        while (lessThan.hasValue(true)) {
            PrimitiveVS<Integer> idxVS = new PrimitiveVS<>(idx).restrict(lessThan.getGuardFor(true));
            sum = sum.increment(idxVS, vc.clock.get(idxVS));
            idx++;
            lessThan = IntegerVS.lessThan(idx, size);
        }
        return sum;
    }

    public VectorClockVS update(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS sum = new VectorClockVS(this);
        PrimitiveVS<Integer> size = vc.size();
        PrimitiveVS<Boolean> lessThan = IntegerVS.lessThan(idx, size);
        while (lessThan.hasValue(true)) {
            PrimitiveVS<Integer> idxVS = new PrimitiveVS<>(idx).restrict(lessThan.getGuardFor(true));
            sum = sum.takeMax(idxVS, vc.clock.get(idxVS));
            idx++;
            lessThan = IntegerVS.lessThan(idx, size);
        }
        return sum;
    }

    // 1 for greater than, 0 for equal, -1 for less than, 2 for incomparable
    public PrimitiveVS<Integer> cmp(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS extended = this.extend(vc.size());
        VectorClockVS extendedVc = vc.extend(this.size());
        PrimitiveVS<Integer> result = new PrimitiveVS<>(0);
        PrimitiveVS<Boolean> inRange = extended.clock.inRange(idx).restrict(vc.getUniverse());
        // compare clocks of the same size
        while (inRange.hasValue(true)) {
            Guard cond = inRange.getGuardFor(true);
            PrimitiveVS<Integer> current = new PrimitiveVS<>(idx).restrict(cond);
            PrimitiveVS<Integer> thisVal = extended.clock.restrict(cond).get(current);
            PrimitiveVS<Integer> otherVal = extendedVc.clock.restrict(cond).get(current);
            PrimitiveVS<Integer> cmp = IntegerVS.compare(thisVal, otherVal);
            result = cmp.apply(result, (a, b) -> {
                        if (a <= 0 && b <= 0) {
                            return Integer.min(a, b);
                        } else if (a >= 0 && b >= 0) {
                            return Integer.max(a, b);
                        } else return 2;
                    });
            idx++;
            inRange = extended.clock.inRange(idx).restrict(vc.getUniverse());
        }
        return result;
    }

    @Override
    public boolean isEmptyVS() {
        return clock.isEmptyVS();
    }

    @Override
    public VectorClockVS restrict(Guard guard) {
        return new VectorClockVS(clock.restrict(guard));
    }

    @Override
    public VectorClockVS merge(Iterable<VectorClockVS> summaries) {
        List<ListVS<PrimitiveVS<Integer>>> summs = new ArrayList<>();
        summaries.forEach(x -> summs.add(x.clock));
        return new VectorClockVS(clock.merge(summs));
    }

    @Override
    public VectorClockVS merge(VectorClockVS summary) {
        return new VectorClockVS(clock.merge(summary.clock));
    }

    @Override
    public VectorClockVS updateUnderGuard(Guard guard, VectorClockVS update) {
        return new VectorClockVS(clock.updateUnderGuard(guard, update.clock));
    }

    @Override
    public PrimitiveVS<Boolean> symbolicEquals(VectorClockVS cmp, Guard pc) {
        return clock.symbolicEquals(cmp.clock, pc);
    }

    @Override
    public Guard getUniverse() {
        return clock.getUniverse();
    }

    @Override
    public String toString() {
        return clock.toString();
    }
}
