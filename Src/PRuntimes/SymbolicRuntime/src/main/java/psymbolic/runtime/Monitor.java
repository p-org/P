package psymbolic.runtime;

import psymbolic.runtime.machine.Machine;
import psymbolic.runtime.machine.State;
import psymbolic.runtime.machine.buffer.EventBufferSemantics;

public class Monitor extends Machine {
    public Monitor(String name, int id, EventBufferSemantics semantics, State startState, State... states) {
        super(name, id, semantics, startState, states);
    }
}
