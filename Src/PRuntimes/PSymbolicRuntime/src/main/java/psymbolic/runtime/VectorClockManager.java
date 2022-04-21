package psymbolic.runtime;

import psymbolic.valuesummary.*;
import psymbolic.runtime.machine.Machine;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.List;

public class VectorClockManager implements Serializable {

    private boolean enabled;

    public VectorClockManager(boolean enabled) {
        this.enabled = enabled;
    }

    public boolean isEnabled() { return enabled; }

    public void enable() { this.enabled = true; }

    public void disable() { this.enabled = false; }

    private MapVS<Machine, PrimitiveVS<Integer>> idxMap = new MapVS<>(Guard.constTrue());

    public void addMachine(Guard cond, Machine m) {
        if (!enabled) return;
        idxMap = idxMap.add(new PrimitiveVS<>(m).restrict(cond), IntegerVS.add(idxMap.getSize(), 1));
    }

    public Guard hasIdx(PrimitiveVS<Machine> m) {
        return idxMap.containsKey(m).getGuardFor(true);
    }

    public PrimitiveVS<Integer> getIdx(PrimitiveVS<Machine> m) {
        if (!enabled) return new PrimitiveVS();
        return idxMap.get(m);
    }

    public static VectorClockVS fromMachineVS(PrimitiveVS<Machine> m) {
        VectorClockVS vc = new VectorClockVS(Guard.constFalse());
        List<VectorClockVS> toMerge = new ArrayList<>();
        for (GuardedValue<Machine> guardedValue : m.getGuardedValues()) {
            toMerge.add(guardedValue.getValue().getClock().restrict(guardedValue.getGuard()));
        }
        return vc.merge(toMerge);
    }
}
