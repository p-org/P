package pexplicit.runtime.machine;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

/**
 * Represents the local state of a machine
 */
public class MachineLocalState implements Serializable {
    /**
     * List of values of all local variables (including internal variables like currentState, FIFO queue, etc.)
     */
    @Getter
    @Setter
    private List<Object> locals;

    public MachineLocalState(List<Object> locals) {
        this.locals = locals;
    }
}
