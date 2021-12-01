package psymbolic.commandline;

import psymbolic.runtime.Event;
import psymbolic.runtime.machine.Monitor;
import psymbolic.runtime.scheduler.Scheduler;
import psymbolic.runtime.machine.Machine;

import java.util.List;
import java.util.Map;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
    Map<Event, List<Monitor>> getMonitorMap();
    List<Monitor> getMonitorList();
}
