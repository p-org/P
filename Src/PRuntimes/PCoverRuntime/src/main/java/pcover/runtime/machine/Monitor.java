package pcover.runtime.machine;

/**
 * Represents a P monitor.
 */
public class Monitor extends Machine {
    /**
     * Monitor constructor
     * @param name Name of the monitor
     * @param id Input id of the monitor
     * @param startState Start state
     * @param states All states of this monitor
     */
    public Monitor(String name, int id, State startState, State... states) {
        super(name, id, startState, states);
        this.instanceId = 0;
        globalMachineId--;
    }

    @Override
    public int compareTo(Machine rhs) {
        return name.compareTo(rhs.getName());
    }

    @Override
    public int hashCode() {
        return name.hashCode();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof Machine)) {
            return false;
        }
        if (this.name == null)
            return (((Machine) obj).name == null);
        return this.name.equals(((Machine) obj).name);
    }

    @Override
    public String toString() {
        return String.format("%s", name);
    }
}
