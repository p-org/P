package pcontainment.runtime.machine;

import com.microsoft.z3.IntExpr;

public class SymbolicMachineIdentifier implements MachineIdentifier {
    public final IntExpr id;

    public SymbolicMachineIdentifier(IntExpr id) {
        if (id == null) {
            throw new RuntimeException("null id");
        }
        this.id = id;
    }
}
