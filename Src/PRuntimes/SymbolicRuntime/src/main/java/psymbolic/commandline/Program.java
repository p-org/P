package psymbolic.run;

import psymbolic.runtime.Machine;
import psymbolic.runtime.Scheduler;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
}
