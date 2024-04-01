package pexplicit.values;

import java.util.Objects;

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
    }

    /**
     * Constructor.
     *
     * @param event PEvent
     */
    public PEvent(PEvent event) {
        this.name = event.name;
    }

    public boolean isCreateMachineEvent() {
        return this.equals(createMachine);
    }

    @Override
    public PEvent clone() {
        return new PEvent(name);
    }

    @Override
    public boolean equals(Object obj) {
        if (obj == this) return true;
        else if (!(obj instanceof PEvent)) {
            return false;
        }
        return this.name.equals(((PEvent) obj).name);
    }

    @Override
    public int hashCode() {
        return Objects.hash(name);
    }

    @Override
    public String toString() {
        return name;
    }
}
