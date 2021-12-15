package p.runtime.events;

public class PEvent {
    private final String name;
    private final int assumeMaxInstances;
    private final int assertMaxInstances;

    /*** Constructors ***/
    public PEvent(String name) {
        this.name = name;
        this.assumeMaxInstances = -1;
        this.assertMaxInstances = -1;
    }

    public PEvent(String name, int assume, int _assert) {
        this.name = name;
        this.assumeMaxInstances = assume;
        this.assertMaxInstances = _assert;
    }

    /*** Getters ***/
    public String getName() { return name; }
    public int getAssumeMaxInstances() { return assumeMaxInstances; }
    public int getAssertMaxInstances() { return assertMaxInstances; }

}
