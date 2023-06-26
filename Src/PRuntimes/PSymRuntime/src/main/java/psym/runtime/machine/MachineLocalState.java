package psym.runtime.machine;

import lombok.Getter;
import psym.valuesummary.ValueSummary;

import java.io.Serializable;
import java.util.List;

public class MachineLocalState implements Serializable {
    @Getter
    List<ValueSummary> locals; // <ValueSummary>
    // Create a constructor that takes a Machine element and popoulates locals as getLocalState() function in Machine.java
    public MachineLocalState(List<ValueSummary> inp_locals) {
        locals = inp_locals;
    }
}
