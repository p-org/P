package pexplicit.runtime.machine;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

public class MachineState implements Serializable {
    @Getter
    @Setter
    private List<Object> locals;

    public MachineState(List<Object> locals) {
        this.locals = locals;
    }
}
