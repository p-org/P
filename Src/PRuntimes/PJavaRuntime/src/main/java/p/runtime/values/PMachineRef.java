package p.runtime.values;

import p.runtime.machines.PMachine;

import java.util.HashSet;
import java.util.Set;

public class PMachineRef extends PValue<PMachineRef> {

    private final PMachine machine;
    private final Set<String> permissions;

    public PMachineRef(PMachine machine)
    {
        this.machine = machine;
        permissions = new HashSet<>();
    }

    public PMachineRef(PMachine machine, Set<String> permissions)
    {
        this.machine = machine;
        this.permissions = permissions;
    }

    @Override
    public PMachineRef clone() {
        Set<String> cPermissions = new HashSet<>(this.permissions);
        return new PMachineRef(machine, cPermissions);
    }

    @Override
    public int hashCode() {
        return machine.hashCode() ^ ComputeHash.getHashCode(permissions);
    }

    @Override
    public boolean equals(Object other) {
        if (other == this)
            return true;
        else if (!(other instanceof PMachineRef)) {
            return false;
        }
        return this.machine == ((PMachineRef) other).machine && this.permissions.equals(((PMachineRef) other).permissions);
    }

    @Override
    public String toString() {
        return String.format("%s (%d)", machine.getName(), machine.getInstanceId());
    }
}
