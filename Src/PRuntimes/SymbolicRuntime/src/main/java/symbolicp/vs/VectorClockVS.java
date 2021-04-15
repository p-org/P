package symbolicp.vs;

import symbolicp.bdd.Bdd;
import symbolicp.runtime.RuntimeLogger;
import symbolicp.runtime.Schedule;
import symbolicp.runtime.ScheduleLogger;

import java.util.ArrayList;
import java.util.List;

public class VectorClockVS implements ValueSummary<VectorClockVS> {

    private final ListVS<PrimVS<Integer>> clock;

    public VectorClockVS(Bdd universe) {
        this.clock = new ListVS<>(universe);
    }

    public VectorClockVS(VectorClockVS vc) {
        this.clock = new ListVS<>(vc.clock);
    }

    private VectorClockVS(ListVS<PrimVS<Integer>> clock) {
        this.clock = clock;
    }

    public PrimVS<Integer> size() {
        return clock.size();
    }

    public VectorClockVS extend (PrimVS<Integer> size) {
        PrimVS<Integer> currentSize = this.size();
        ListVS<PrimVS<Integer>> extended = clock;
        PrimVS<Boolean> lessThan = IntUtils.lessThan(currentSize, size);
        while (lessThan.hasValue(true)) {
            Bdd lessThanCond = lessThan.getGuard(true);
            extended = extended.add(new PrimVS<>(0).guard(lessThanCond));
            currentSize = extended.size();
            lessThan = IntUtils.lessThan(currentSize, size);
        }
        return new VectorClockVS(extended);
    }

    public VectorClockVS increment(PrimVS<Integer> idx, PrimVS<Integer> amt) {
        ListVS<PrimVS<Integer>> updatedClock = extend(IntUtils.add(idx, 1)).clock;
        PrimVS<Boolean> inRange = updatedClock.inRange(idx);
        Bdd inRangeCond = inRange.getGuard(true);
        PrimVS<Integer> updateValue = IntUtils.add(updatedClock.get(idx.guard(inRangeCond)), amt.guard(inRangeCond));
        updatedClock = updatedClock.set(idx.guard(inRangeCond), updateValue);
        return new VectorClockVS(updatedClock);
    }

    public VectorClockVS increment(PrimVS<Integer> idx) {
        return increment(idx, new PrimVS<>(1));
    }

    public VectorClockVS takeMax(PrimVS<Integer> idx, PrimVS<Integer> amt) {
        ListVS<PrimVS<Integer>> updatedClock = extend(IntUtils.add(idx, 1)).clock;
        PrimVS<Boolean> inRange = updatedClock.inRange(idx);
        Bdd inRangeCond = inRange.getGuard(true);
        PrimVS<Integer> cmpResult = IntUtils.compare(updatedClock.get(idx.guard(inRangeCond)), amt.guard(inRangeCond));
        Bdd updateCond = IntUtils.lessThan(cmpResult, 0).getGuard(true);
        updatedClock = updatedClock.set(idx.guard(inRangeCond).guard(updateCond), amt);
        return new VectorClockVS(updatedClock);
    }

    public VectorClockVS add(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS sum = new VectorClockVS(this);
        PrimVS<Integer> size = vc.size();
        PrimVS<Boolean> lessThan = IntUtils.lessThan(idx, size);
        while (lessThan.hasValue(true)) {
            PrimVS<Integer> idxVS = new PrimVS<>(idx).guard(lessThan.getGuard(true));
            sum = sum.increment(idxVS, vc.clock.get(idxVS));
            idx++;
            lessThan = IntUtils.lessThan(idx, size);
        }
        return sum;
    }

    public VectorClockVS update(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS sum = new VectorClockVS(this);
        PrimVS<Integer> size = vc.size();
        PrimVS<Boolean> lessThan = IntUtils.lessThan(idx, size);
        while (lessThan.hasValue(true)) {
            PrimVS<Integer> idxVS = new PrimVS<>(idx).guard(lessThan.getGuard(true));
            sum = sum.takeMax(idxVS, vc.clock.get(idxVS));
            idx++;
            lessThan = IntUtils.lessThan(idx, size);
        }
        return sum;
    }

    // 1 for greater than, 0 for equal, -1 for less than, 2 for incomparable
    public PrimVS<Integer> cmp(VectorClockVS vc) {
        int idx = 0;
        VectorClockVS extended = this.extend(vc.size());
        VectorClockVS extendedVc = vc.extend(this.size());
        PrimVS<Integer> result = new PrimVS<>(0);
        PrimVS<Boolean> inRange = extended.clock.inRange(idx);
        // compare clocks of the same size
        while (inRange.hasValue(true)) {
            Bdd cond = inRange.getGuard(true);
            PrimVS<Integer> current = new PrimVS<>(idx).guard(cond);
            PrimVS<Integer> thisVal = extended.clock.guard(cond).get(current);
            PrimVS<Integer> otherVal = extendedVc.clock.guard(cond).get(current);
            PrimVS<Integer> cmp = IntUtils.compare(thisVal, otherVal);
            result = cmp.apply2(result, (a, b) -> {
                        if (a <= 0 && b <= 0) {
                            return Integer.min(a, b);
                        } else if (a >= 0 && b >= 0) {
                            return Integer.max(a, b);
                        } else return 2;
                    });
            idx++;
            inRange = extended.clock.inRange(idx);
        }
        return result;
    }

    @Override
    public boolean isEmptyVS() {
        return clock.isEmptyVS();
    }

    @Override
    public VectorClockVS guard(Bdd guard) {
        return new VectorClockVS(clock.guard(guard));
    }

    @Override
    public VectorClockVS merge(Iterable<VectorClockVS> summaries) {
        List<ListVS<PrimVS<Integer>>> summs = new ArrayList<>();
        summaries.forEach(x -> summs.add(x.clock));
        return new VectorClockVS(clock.merge(summs));
    }

    @Override
    public VectorClockVS merge(VectorClockVS summary) {
        return new VectorClockVS(clock.merge(summary.clock));
    }

    @Override
    public VectorClockVS update(Bdd guard, VectorClockVS update) {
        return new VectorClockVS(clock.update(guard, update.clock));
    }

    @Override
    public PrimVS<Boolean> symbolicEquals(VectorClockVS cmp, Bdd pc) {
        return clock.symbolicEquals(cmp.clock, pc);
    }

    @Override
    public Bdd getUniverse() {
        return clock.getUniverse();
    }

    @Override
    public String toString() {
        return clock.toString();
    }
}
