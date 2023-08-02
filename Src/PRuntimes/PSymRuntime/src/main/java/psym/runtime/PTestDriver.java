package psym.runtime;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.events.Event;

public abstract class PTestDriver implements Serializable {
    public Machine mainMachine;
    public List<Monitor> monitorList;
    public Map<Event, List<Monitor>> observerMap;

    public PTestDriver() {
        this.mainMachine = null;
        this.monitorList = new ArrayList<>();
        this.observerMap = new HashMap<>();
        this.configure();
    }

    public Machine getStart() {
        return mainMachine;
    }

    public List<Monitor> getMonitors() {
        return monitorList;
    }

    public Map<Event, List<Monitor>> getListeners() {
        return observerMap;
    }

    public abstract void configure();
}