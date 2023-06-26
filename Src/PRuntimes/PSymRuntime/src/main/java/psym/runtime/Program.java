package psym.runtime;

import java.io.Serializable;
import java.util.List;
import java.util.Map;
import psym.runtime.machine.Machine;
import psym.runtime.machine.Monitor;
import psym.runtime.machine.events.Event;
import psym.runtime.scheduler.Scheduler;

public interface Program extends Serializable {
    Machine getStart();

    Scheduler getProgramScheduler();

    void setProgramScheduler(Scheduler s);

    Map<Event, List<Monitor>> getListeners();

    List<Monitor> getMonitors();

    PTestDriver getTestDriver();

    void setTestDriver(PTestDriver input);
}
