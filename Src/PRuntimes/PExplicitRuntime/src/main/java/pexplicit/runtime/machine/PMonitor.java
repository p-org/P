package pexplicit.runtime.machine;

/**
 * Represents a P monitor.
 */
public class PMonitor extends PMachine {
    /**
     * Monitor constructor
     *
     * @param name       Name of the monitor
     * @param id         Input id of the monitor
     * @param startState Start state
     * @param states     All states of this monitor
     */
    public PMonitor(String name, int id, State startState, State... states) {
        super(name, id, startState, states);
        this.instanceId = 0;
        globalMachineId--;
    }

    @Override
    public int hashCode() {
        return name.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PMachine)) {
            return false;
        }
        if (this.name == null)
            return (((PMachine) obj).name == null);
        return this.name.equals(((PMachine) obj).name);
    }

    @Override
    public String toString() {
        return String.format("%s", name);
    }
}
