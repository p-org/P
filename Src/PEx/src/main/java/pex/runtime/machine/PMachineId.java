package pex.runtime.machine;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
public class PMachineId implements Serializable {
    Class<? extends PMachine> type;
    int typeId;
    @Setter
    String name;

    public PMachineId(Class<? extends PMachine> t, int tid) {
        type = t;
        typeId = tid;
        name = null;
    }


    @Override
    public String toString() {
        return name;
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
