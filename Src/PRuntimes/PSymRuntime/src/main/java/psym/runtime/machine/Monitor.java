package psym.runtime.machine;

public class Monitor extends Machine {
    public Monitor(String name, int id, State startState, State... states) {
        super(name, id, startState, states);
    }
}
