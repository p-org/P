package pcontainment.commandline;

import pcontainment.runtime.Event;
import pcontainment.runtime.scheduler.Scheduler;
import pcontainment.runtime.machine.Machine;

import java.util.List;
import java.util.Map;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
    Map<Event, List<Monitor>> getMonitorMap();
    List<Monitor> getMonitorList();
}
