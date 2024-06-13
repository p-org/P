package pexplicit.runtime.scheduler.choice;

import pexplicit.runtime.machine.PMachineId;

public class ScheduleChoice extends Choice<PMachineId> {
    public ScheduleChoice(PMachineId value) {
        super(value);
    }

    @Override
    public String toString() {
        return value.toString();
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof ScheduleChoice)) {
            return false;
        }
        return this.value == ((ScheduleChoice) obj).value;
    }
}
