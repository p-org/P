package symbolicp.runtime;

import symbolicp.bdd.Bdd;
import symbolicp.vs.GuardedValue;
import symbolicp.vs.PrimVS;

import java.util.ArrayList;
import java.util.List;

public class TransDPORScheduler extends BoundedScheduler {

    @Override
    public Schedule getNewSchedule() {
        return new DporSchedule();
    }

    public TransDPORScheduler(String name, int senderBound, int boolBound, int intBound) {
        super("trans_" + name, senderBound, boolBound, intBound);
    }

    @Override
    public void step() {
        PrimVS<Machine> enabled = new PrimVS();
        // get latest scheduling choice
        for (int i = schedule.size() - 1; i >= 0; i--) {
            enabled = schedule.getSenderChoice(i);
            if (!enabled.isEmptyVS()) break;
        }
        ((DporSchedule) schedule).compare(enabled);
        super.step();
    }
}
