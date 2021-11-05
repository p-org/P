package psymbolic.runtime.machine;

import psymbolic.runtime.machine.buffer.EventBufferSemantics;

public class Monitor extends Machine {
    public Monitor(String name, int id, EventBufferSemantics semantics, State startState, State... states) {
        super(name, id, semantics, startState, states);
    }
}
