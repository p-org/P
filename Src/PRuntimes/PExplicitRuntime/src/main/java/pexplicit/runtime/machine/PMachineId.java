package pexplicit.runtime.machine;

import lombok.Getter;

@Getter
public class PMachineId {
    Class<? extends PMachine> type;
    int typeId;

    public PMachineId(Class<? extends PMachine> t, int tid) {
        type = t;
        typeId = tid;
    }
}
