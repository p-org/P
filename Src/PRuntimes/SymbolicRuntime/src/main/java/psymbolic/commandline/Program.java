package psymbolic.commandline;

import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.Scheduler;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
}
