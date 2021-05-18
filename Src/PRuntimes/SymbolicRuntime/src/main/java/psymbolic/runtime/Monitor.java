package psymbolic.runtime;

public class Monitor extends Machine {
    public Monitor(String name, int id, BufferSemantics semantics, State startState, State... states) {
        super(name, id, semantics, startState, states);
    }
}
