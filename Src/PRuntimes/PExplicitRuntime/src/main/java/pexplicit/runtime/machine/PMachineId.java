package pexplicit.runtime.machine;

import lombok.Getter;
import pexplicit.runtime.scheduler.choice.ScheduleChoice;

@Getter
public class PMachineId {
    Class<? extends PMachine> type;
    int typeId;

    public PMachineId(Class<? extends PMachine> t, int tid) {
        type = t;
        typeId = tid;
    }

    @Override
    public String toString() {
        return String.format("%s<%d>", type.getSimpleName(), typeId);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this)
            return true;
        else if (!(obj instanceof PMachineId)) {
            return false;
        }
        PMachineId rhs = (PMachineId) obj;
        return this.type == rhs.type && this.typeId == rhs.typeId;
    }
}
