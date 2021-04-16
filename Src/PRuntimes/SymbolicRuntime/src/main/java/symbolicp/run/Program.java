package symbolicp.run;

import symbolicp.runtime.Machine;
import symbolicp.runtime.Scheduler;

public interface Program {
    Machine getStart();
    void setScheduler(Scheduler s);
}
