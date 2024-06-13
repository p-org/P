package pexplicit.values;

import lombok.Getter;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.PMachineId;
import pexplicit.runtime.machine.PMonitor;
import pexplicit.utils.exceptions.BugFoundException;

/**
 * Represents the PValue for P machine
 */
@Getter
public class PMachineValue extends PValue<PMachineValue> {
    private final PMachineId pid;
    private final String name;

    /**
     * Constructor
     *
     * @param val machine value to set to
     */
    public PMachineValue(PMachine val) {
        pid = val.getPid();
        name =  val.toString();
        initialize();
    }

    private PMachineValue(PMachineId inp_pid, String inp_name) {
        pid = inp_pid;
        name = inp_name;
    }

    public PMachine getValue() {
        return PExplicitGlobal.getGlobalMachine(pid);
    }

    /**
     * Get the unique machine identifier
     *
     * @return unique machine instance id
     */
    public int getId() {
        PMachine value = getValue();
        if (value instanceof PMonitor) {
            throw new BugFoundException(String.format("Cannot fetch id from a PMonitor: %s", value));
        }
        return value.getInstanceId();
    }

    @Override
    public PMachineValue clone() {
        return new PMachineValue(pid, name);
    }

    @Override
    protected String _asString() {
        return name;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PMachineValue)) {
            return false;
        }
        return this.pid.equals(((PMachineValue) obj).pid);
    }
}
