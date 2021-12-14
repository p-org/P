package p.runtime.events;

import p.runtime.values.PMachineRef;

public abstract class PEvent {

    private final String name;
    private final PMachineRef source;
    private final PMachineRef target;
    private final int assumeMaxInstances;
    private final int assertMaxInstances;

    /*** Constructors ***/
    public PEvent(String name) {
        this.name = name;
        source = null;
        target = null;
        assertMaxInstances = -1;
        assumeMaxInstances = -1;
    }

    public PEvent(String name, PMachineRef source, PMachineRef target) {
        this.name = name;
        this.source = null;
        this.target = null;
        assertMaxInstances = -1;
        assumeMaxInstances = -1;
    }
    /*** Getters ***/
    public String getName() { return name; }
    public PMachineRef getSource() { return source; }
    public PMachineRef getTarget() { return target; }
    public int getAssumeMaxInstances() { return assumeMaxInstances; }
    public int getAssertMaxInstances() { return assertMaxInstances; }
}
