package pexplicit.values;

/**
 * Represents a P event
 */
public class PEvent extends PValue<PEvent> {
    /**
     * Special event send to a machine on creation
     */
    public static final PEvent createMachine = new PEvent("createMachine");

    /**
     * Special halt event
     */
    public static final PEvent haltEvent = new PEvent("_halt");

    /**
     * Name of the event
     */
    final String name;

    /**
     * Constructor.
     *
     * @param name PEvent name
     */
    public PEvent(String name) {
        this.name = name;
        initialize();
    }

    /**
     * Constructor.
     *
     * @param event PEvent
     */
    public PEvent(PEvent event) {
        this.name = event.name;
        initialize();
    }

    public boolean isCreateMachineEvent() {
        return this.equals(createMachine);
    }

    public boolean isHaltMachineEvent() {
        return this.equals(haltEvent);
    }

    @Override
    public PEvent clone() {
        return new PEvent(name);
    }

    @Override
    protected String _asString() {
        return name;
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PEvent)) {
            return false;
        }
        return this.name.equals(((PEvent) obj).name);
    }
}
