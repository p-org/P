package psym.runtime.scheduler.search.symmetry;

import lombok.Getter;
import psym.runtime.machine.Machine;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;

public class SymmetryPendingMerges {
    @Getter
    PrimitiveVS<Machine> pendingMachines;

    public SymmetryPendingMerges() {
        pendingMachines = new PrimitiveVS<>();
    }

    public void add(PrimitiveVS<Machine> machineVS) {
        pendingMachines = pendingMachines.merge(machineVS);
    }

    public Guard getUniverse() {
        return pendingMachines.getUniverse();
    }

}
