package psymbolic.run;

import psymbolic.runtime.Machine;
import psymbolic.runtime.Scheduler;
import psymbolic.runtime.Monitor;
import psymbolic.runtime.EventName;

import java.util.List;
import java.util.Map;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
    Map<EventName, List<Monitor>> getMonitorMap();
    List<Monitor> getMonitorList();
}
