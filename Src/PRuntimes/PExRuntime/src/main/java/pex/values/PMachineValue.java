package pex.values;

import lombok.Getter;
import pex.runtime.machine.PMachine;
import pex.runtime.machine.PMonitor;
import pex.utils.exceptions.BugFoundException;

/**
 * Represents the PValue for P machine
 */
@Getter
public class PMachineValue extends PValue<PMachineValue> {
    private final PMachine value;

    /**
     * Constructor
     *
     * @param val machine value to set to
     */
    public PMachineValue(PMachine val) {
        value = val;
        initialize();
    }

    /**
     * Get the unique machine identifier
     *
     * @return unique machine instance id
     */
    public int getId() {
        if (value instanceof PMonitor) {
            throw new BugFoundException(String.format("Cannot fetch id from a PMonitor: %s", value));
        }
        return value.getInstanceId();
    }

    @Override
    public PMachineValue clone() {
        return new PMachineValue(value);
    }

    @Override
    protected String _asString() {
        return value.toString();
    }

    @Override
    public PMachineValue getDefault() {
        return null;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PMachineValue)) {
            return false;
        }
        return this.value.equals(((PMachineValue) obj).value);
    }
}
