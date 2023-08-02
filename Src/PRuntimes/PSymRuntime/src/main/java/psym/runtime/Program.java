package psym.runtime;

import java.io.Serializable;
import java.util.List;
import java.util.Map;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.events.Event;

public interface Program extends Serializable {
    Machine getStart();

    Map<Event, List<Monitor>> getListeners();

    List<Monitor> getMonitors();

    PTestDriver getTestDriver();

    void setTestDriver(PTestDriver input);
}
