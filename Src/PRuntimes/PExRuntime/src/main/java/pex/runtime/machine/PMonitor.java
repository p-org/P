package pex.runtime.machine;

import pex.runtime.PExGlobal;

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
        PExGlobal.setGlobalMonitorId(PExGlobal.getGlobalMonitorId() - 1);
        this.instanceId = PExGlobal.getGlobalMonitorId();
    }

    @Override
    public String toString() {
        return String.format("%s", name);
    }
}
