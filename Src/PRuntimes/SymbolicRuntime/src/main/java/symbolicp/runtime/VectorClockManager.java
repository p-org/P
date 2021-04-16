package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.*;

import java.util.ArrayList;
import java.util.List;

public class VectorClockManager {

    public boolean enabled;

    public VectorClockManager(boolean enabled) {
        this.enabled = enabled;
    }

    public boolean isEnabled() { return enabled; }

    private MapVS<Machine, PrimVS<Integer>> idxMap = new MapVS<>(Bdd.constTrue());

    public void addMachine(Bdd cond, Machine m) {
        if (!enabled) return;
        //ScheduleLogger.log("add machine at index " + IntUtils.add(idxMap.getSize(), 1));
        idxMap = idxMap.add(new PrimVS<>(m).guard(cond), IntUtils.add(idxMap.getSize(), 1));
        getIdx(new PrimVS<>(m));
    }

    public PrimVS<Integer> getIdx(PrimVS<Machine> m) {
        if (!enabled) return new PrimVS();
        //ScheduleLogger.log("get idx of " + m.toString());
        //ScheduleLogger.log("contains key:" + idxMap.containsKey(m));
        for (Machine mach : m.getValues()) {
            //ScheduleLogger.log("(test) get idx of " + mach.toString());
            idxMap.get(new PrimVS<>(mach));
            //ScheduleLogger.log("contains key:" + idxMap.containsKey(new PrimVS<>(mach)));
            //ScheduleLogger.log("(test) got idx of " + mach.toString());
        }
        return idxMap.get(m);
    }

    public static VectorClockVS fromMachineVS(PrimVS<Machine> m) {
        VectorClockVS vc = new VectorClockVS(Bdd.constFalse());
        List<VectorClockVS> toMerge = new ArrayList<>();
        for (GuardedValue<Machine> guardedValue : m.getGuardedValues()) {
            toMerge.add(guardedValue.value.getClock().guard(guardedValue.guard));
        }
        return vc.merge(toMerge);
    }
}
